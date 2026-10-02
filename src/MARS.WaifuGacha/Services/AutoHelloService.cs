using MARS.Shared.Concurrency;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Services;

/// <summary>
/// Генератор приветствий от супруга (auto-hello).
/// Портирован из монолита <c>MARS.Server/Services/WaifuRoll/WaifuRollService.AutoHello</c>.
/// Условия отправки: пользователь в браке, приветствия включены,
/// с предыдущего приветствия прошло не меньше 20 часов.
/// Если наступила годовщина свадьбы — отправляется поздравление,
/// иначе — случайная фраза из <see cref="AutoHelloMessage"/>.
/// </summary>
public class AutoHelloService(
    IDbContextFactory<WaifuDbContext> factory,
    WeddingAnniversaryService anniversaryService,
    KeyedAsyncLock keyedLock,
    ILogger<AutoHelloService> logger
)
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromHours(20);

    private const string RandomHostToken = "{randomHost}";

    private const string RandomHostFallback = "один из зрителей";

    private const string DefaultSpouseName = "супруг(а)";

    public async Task<string?> GetAutoHelloMessageAsync(
        string twitchId,
        string displayName,
        CancellationToken cancellationToken = default
    )
    {
        string? result = null;

        if (!string.IsNullOrWhiteSpace(twitchId) && !string.IsNullOrWhiteSpace(displayName))
        {
            using var handle = await keyedLock.AcquireAsync(twitchId, cancellationToken);

            try
            {
                result = await BuildMessageAsync(twitchId, displayName, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Ошибка при формировании auto-hello для {TwitchId}",
                    twitchId
                );
            }
        }

        return result;
    }

    /// <summary>
    /// Переключает флаг авто-приветствия и возвращает новое состояние.
    /// Если пользователя ещё нет в браке-таблице, создаёт запись и включает приветствие.
    /// </summary>
    public async Task<bool> ToggleAutoHelloEnabledAsync(
        string twitchId,
        CancellationToken cancellationToken = default
    )
    {
        var result = false;

        if (!string.IsNullOrWhiteSpace(twitchId))
        {
            using var handle = await keyedLock.AcquireAsync(twitchId, cancellationToken);

            try
            {
                await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

                var host = await dbContext.Husbands.FirstOrDefaultAsync(
                    e => e.TwitchId == twitchId,
                    cancellationToken
                );

                if (host is null)
                {
                    dbContext.Husbands.Add(
                        new Husband
                        {
                            TwitchId = twitchId,
                            HusbandCoolDown = new HusbandCoolDown { HusbandId = twitchId },
                            HusbandGreetings = new HusbandAutoHello
                            {
                                HusbandId = twitchId,
                                Time = DateTime.UtcNow,
                            },
                            IsAutoHelloEnabled = true,
                        }
                    );

                    await dbContext.SaveChangesAsync(cancellationToken);
                    result = true;
                }
                else
                {
                    result = !host.IsAutoHelloEnabled;

                    await dbContext
                        .Husbands.Where(e => e.TwitchId == twitchId)
                        .ExecuteUpdateAsync(
                            setters => setters.SetProperty(e => e.IsAutoHelloEnabled, result),
                            cancellationToken
                        );
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Ошибка при переключении auto-hello для {TwitchId}",
                    twitchId
                );
                result = false;
            }
        }

        return result;
    }

    private async Task<string?> BuildMessageAsync(
        string twitchId,
        string displayName,
        CancellationToken cancellationToken
    )
    {
        string? result = null;

        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

        var host = await dbContext
            .Husbands.Include(e => e.HusbandGreetings)
            .Include(e => e.HusbandCoolDown)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.TwitchId == twitchId, cancellationToken);

        if (host is not { IsPrivated: true, IsAutoHelloEnabled: true })
        {
            return null;
        }

        var greet = host.HusbandGreetings;
        var isNewGreeting = greet is null;
        var shouldGreet = greet is null || greet.Time <= DateTime.UtcNow - Cooldown;

        if (!shouldGreet)
        {
            return null;
        }

        var anniversary = await anniversaryService.GetNextUnsentAnniversaryAsync(
            twitchId,
            cancellationToken
        );

        if (anniversary.HasValue && host.WaifuBrideId != null)
        {
            var spouseName = await GetSpouseNameAsync(
                dbContext,
                host.WaifuBrideId,
                cancellationToken
            );

            result = WeddingAnniversaryService.BuildCongratulationMessageFromSpouse(
                displayName,
                spouseName,
                anniversary.Value
            );
        }
        else if (host.WaifuBrideId != null)
        {
            var spouseName = await GetSpouseNameAsync(
                dbContext,
                host.WaifuBrideId,
                cancellationToken
            );
            var helloText = await GetHelloTextAsync(dbContext, cancellationToken);
            var fixedText = ResolveRandomHost(helloText);

            result = ReplaceKeywordsInAnswer(
                displayName,
                $"@{{user}}, твой супруг {spouseName} прислал(а) тебе сообщение: \"{fixedText}\"",
                spouseName
            );
        }

        if (result is not null)
        {
            await SaveGreetingAsync(twitchId, greet, isNewGreeting, cancellationToken);
        }

        return result;
    }

    private static async Task<string> GetSpouseNameAsync(
        WaifuDbContext dbContext,
        string waifuShikiId,
        CancellationToken cancellationToken
    )
    {
        var name = await dbContext
            .Waifus.AsNoTracking()
            .Where(w => w.ShikiId == waifuShikiId)
            .Select(w => w.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(name) ? DefaultSpouseName : name;
    }

    private async Task SaveGreetingAsync(
        string twitchId,
        HusbandAutoHello? greet,
        bool isNewGreeting,
        CancellationToken cancellationToken
    )
    {
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

        if (isNewGreeting || greet is null)
        {
            dbContext.HusbandGreetings.Add(
                new HusbandAutoHello { HusbandId = twitchId, Time = DateTime.UtcNow }
            );
        }
        else
        {
            var affected = await dbContext
                .HusbandGreetings.Where(e => e.Guid == greet.Guid)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(e => e.Time, DateTime.UtcNow),
                    cancellationToken
                );

            if (affected == 0)
            {
                dbContext.HusbandGreetings.Add(
                    new HusbandAutoHello { HusbandId = twitchId, Time = DateTime.UtcNow }
                );
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<string> GetHelloTextAsync(
        WaifuDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var count = await dbContext.AutoHelloMessages.CountAsync(cancellationToken);
        string result = string.Empty;

        if (count > 0)
        {
            var index = Random.Shared.Next(count);

            result = (
                await dbContext
                    .AutoHelloMessages.OrderBy(e => e.Order)
                    .Skip(index)
                    .FirstAsync(cancellationToken)
            ).Text;
        }

        return result;
    }

    /// <summary>
    /// Подставляет вместо <c>{randomHost}</c> нейтральную фразу.
    /// WaifuGacha не владеет таблицей Twitch-пользователей, поэтому вместо
    /// <c>DisplayName</c> используется безопасная подстановка — иначе в чат
    /// утек бы необработанный токен.
    /// </summary>
    private static string ResolveRandomHost(string message)
    {
        string result = message;

        if (!string.IsNullOrWhiteSpace(message) && message.Contains(RandomHostToken))
        {
            result = message.Replace(RandomHostToken, RandomHostFallback);
        }

        return result;
    }

    private static string ReplaceKeywordsInAnswer(
        string displayName,
        string message,
        string spouseName
    )
    {
        string result = message;

        if (string.IsNullOrWhiteSpace(result))
        {
            return result;
        }

        result = result.Replace("{user}", displayName);
        result = result.Replace("{waifuName}", spouseName);
        result = result.Replace("{waifuTitle}", spouseName);

        return result;
    }
}
