using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MARS.WaifuGacha.Services;

/// <summary>
/// Антиспам-кулдаун роллов (таблица <c>waifu.RollCooldowns</c>).
/// </summary>
/// <remarks>
/// Раньше метод на любой ошибке БД возвращал <c>(true, TimeSpan.Zero)</c>, то есть
/// fail-open: именно тогда, когда база лежит и повторные роллы идут чаще всего,
/// защита молча отключалась. Теперь ошибка БД означает «ролл запрещён» (fail-closed),
/// а сам факт ошибки логируется и попадает в метрику.
/// </remarks>
public class RollCooldownService(
    IDbContextFactory<WaifuDbContext> factory,
    ILogger<RollCooldownService> logger
)
{
    private const int MaxRetries = 2;

    /// <summary>
    /// Проверяет кулдаун и, если он истёк, сразу продлевает его.
    /// </summary>
    /// <param name="twitchUserId">Twitch ID пользователя.</param>
    /// <param name="rollType">Тип ролла (<see cref="RootStateKeys.WaifuRollType"/> и т. п.).</param>
    /// <param name="cooldown">Длительность кулдауна.</param>
    /// <returns>
    /// <c>(true, TimeSpan.Zero)</c> — ролл разрешён; <c>(false, remaining)</c> — запрещён;
    /// при недоступной БД возвращается запрет на полный <paramref name="cooldown"/>.
    /// </returns>
    public async Task<(bool allowed, TimeSpan remaining)> CheckAndUpdateCooldownAsync(
        string twitchUserId,
        string rollType,
        TimeSpan cooldown,
        CancellationToken cancellationToken = default
    )
    {
        (bool allowed, TimeSpan remaining) result = (false, cooldown);

        if (!string.IsNullOrWhiteSpace(twitchUserId))
        {
            for (var attempt = 0; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    result = await TryCheckAndUpdateAsync(
                        twitchUserId,
                        rollType,
                        cooldown,
                        cancellationToken
                    );
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (PostgresException exception)
                    when (exception.SqlState == "23505" && attempt < MaxRetries)
                {
                    logger.LogDebug(
                        exception,
                        "Дубликат записи кулдауна для {UserId}/{RollType}, повтор {Attempt}",
                        twitchUserId,
                        rollType,
                        attempt + 1
                    );
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Ошибка БД при проверке кулдауна {RollType} для {UserId}, ролл запрещён",
                        rollType,
                        twitchUserId
                    );
                    result = (false, cooldown);
                    break;
                }
            }
        }

        return result;
    }

    private async Task<(bool allowed, TimeSpan remaining)> TryCheckAndUpdateAsync(
        string twitchUserId,
        string rollType,
        TimeSpan cooldown,
        CancellationToken cancellationToken
    )
    {
        (bool allowed, TimeSpan remaining) result = (false, cooldown);

        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

        var existing = await dbContext
            .RollCooldowns.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.TwitchUserId == twitchUserId && r.RollType == rollType,
                cancellationToken
            );

        if (existing is not null)
        {
            var elapsed = DateTime.UtcNow - existing.LastRollTime;

            if (elapsed < cooldown)
            {
                result = (false, cooldown - elapsed);
            }
            else
            {
                existing.LastRollTime = DateTime.UtcNow;
                dbContext.RollCooldowns.Update(existing);
                await dbContext.SaveChangesAsync(cancellationToken);
                result = (true, TimeSpan.Zero);
            }
        }
        else
        {
            dbContext.RollCooldowns.Add(
                new RollCooldown
                {
                    TwitchUserId = twitchUserId,
                    RollType = rollType,
                    LastRollTime = DateTime.UtcNow,
                }
            );
            await dbContext.SaveChangesAsync(cancellationToken);
            result = (true, TimeSpan.Zero);
        }

        return result;
    }
}
