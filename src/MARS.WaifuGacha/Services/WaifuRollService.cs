using MARS.Shared.Concurrency;
using MARS.Shared.Models;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using MARS.WaifuGacha.Models;
using Microsoft.EntityFrameworkCore;
using ShikimoriSharp.Classes;

namespace MARS.WaifuGacha.Services;

public class WaifuRollService(
    IDbContextFactory<WaifuDbContext> factory,
    ILogger<WaifuRollService> logger,
    WaifuRollEnsurenceService waifuDbHelper,
    KeyedAsyncLock keyedLock,
    RollCooldownConfigurationService cooldownConfiguration
)
{
    public async Task<Waifu?> RollTheWaifu(
        string id,
        string? displayName = null,
        bool forcePass = false
    )
    {
        Waifu? result = null;

        if (!string.IsNullOrWhiteSpace(id))
        {
            using var handle = await keyedLock.AcquireAsync(id);

            try
            {
                var pass = false;

                await using var dbContext = await factory.CreateDbContextAsync();
                var host = dbContext
                    .Husbands.Include(e => e.HusbandCoolDown)
                    .FirstOrDefault(e => e.TwitchId == id);
                var cd = host?.HusbandCoolDown;
                if (host != null)
                {
                    if (cd is not null)
                    {
                        if (cd.HusbandId == host.TwitchId)
                        {
                            var now = DateTime.UtcNow;
                            var cdTime = cd.Time;
                            var cdFromEnv = await GetWaifuRollCoolDownAsync();
                            var isCDed = now - cdTime >= cdFromEnv;
                            if (isCDed)
                            {
                                pass = true;
                            }
                        }
                        else
                        {
                            cd.HusbandId = host.TwitchId;
                            cd.Time = DateTime.UtcNow;
                            dbContext.HusbandCoolDowns.Update(cd);
                            pass = true;
                        }
                    }
                    else
                    {
                        cd = new HusbandCoolDown { HusbandId = id };
                        host.HusbandCoolDown = cd;
                        dbContext.HusbandCoolDowns.Add(cd);
                        pass = true;
                    }
                }
                else
                {
                    cd = new HusbandCoolDown { HusbandId = id };
                    host = new Husband
                    {
                        TwitchId = id,
                        HusbandGreetings = new HusbandAutoHello { HusbandId = id },
                        HusbandCoolDown = cd,
                    };
                    await dbContext.Husbands.AddAsync(host);
                    pass = true;
                }

                await dbContext.SaveChangesAsync();

                if (forcePass)
                {
                    pass = true;
                }

                if (pass)
                {
                    var waifu = dbContext
                        .Waifus.OrderBy(e => e.LastOrder)
                        .Take(10)
                        .ToList()
                        .OrderBy(x => Random.Shared.Next())
                        .FirstOrDefault();

                    if (waifu != null)
                    {
                        host.WaifuRollId = waifu.ShikiId;
                        host.WhenOrdered = DateTime.UtcNow;

                        if (!forcePass)
                        {
                            host.OrderCount++;
                            waifu.OrderCount++;
                        }

                        host.HusbandCoolDown ??= new HusbandCoolDown()
                        {
                            HusbandId = host.TwitchId,
                        };

                        host.HusbandCoolDown.Time = DateTime.UtcNow;
                        waifu.LastOrder = DateTime.UtcNow;

                        dbContext.Waifus.Update(waifu);
                        cd.Time = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync();

                        result = waifu;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error rolling waifu for user {Id}", id);
            }
        }

        return result;
    }

    public async Task<OperationResult<TelegramRollWaifuResponse>> TelegramRollWaifu(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return OperationResult<TelegramRollWaifuResponse>.Fail(
                "Имя хоста не может быть пустым"
            );
        }

        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();

            var host = await dbContext.Husbands.FirstOrDefaultAsync(e => e.TwitchId.Contains(name));

            if (host is null)
            {
                return OperationResult<TelegramRollWaifuResponse>.Fail("Хост не найден");
            }

            var waifu = await RollTheWaifu(host.TwitchId, host.TwitchId, true);

            var response = new TelegramRollWaifuResponse
            {
                Waifu = waifu,
                Host = host,
                Husband = null,
            };

            if (waifu is { IsPrivated: true })
            {
                var husband = await dbContext.Husbands.FirstAsync(e =>
                    e.WaifuBrideId == waifu.ShikiId
                );
                response.Husband = husband;
            }

            if (
                response.Husband is null
                && host.IsPrivated
                && !string.IsNullOrWhiteSpace(host.WaifuBrideId)
            )
            {
                response.Husband = await dbContext.Husbands.FirstOrDefaultAsync(e =>
                    e.WaifuBrideId == host.WaifuBrideId
                );
            }

            return OperationResult<TelegramRollWaifuResponse>.Ok(response);
        }
        catch (Exception ex)
        {
            return OperationResult<TelegramRollWaifuResponse>.Fail(
                $"Ошибка при ролле вайфу: {ex.Message}"
            );
        }
    }

    public async Task<bool> MergeTheWaifu(Husband? host, Waifu? waifu, bool makeprivate = true)
    {
        var result = false;

        if (host != null && waifu != null)
        {
            await using var dbContext = await factory.CreateDbContextAsync();

            if (makeprivate)
            {
                waifu.IsPrivated = true;
                host.IsPrivated = true;
                host.WaifuBrideId = waifu.ShikiId;
                host.WhenPrivated = DateTime.UtcNow;
            }
            else
            {
                waifu.IsPrivated = false;
                host.IsPrivated = false;
            }

            dbContext.Waifus.Update(waifu);
            dbContext.Husbands.Update(host);

            result = await dbContext.SaveChangesAsync() != 0;
        }

        return result;
    }

    public async Task<OperationResult<AddNewWaifuResponse>> AddNewWaifu(FullCharacter? character)
    {
        if (character is null)
        {
            return OperationResult<AddNewWaifuResponse>.Fail("Персонаж не может быть null");
        }

        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();

            if (dbContext.Waifus.Any(e => e.ShikiId == character.Id.ToString()))
            {
                return OperationResult<AddNewWaifuResponse>.Fail(
                    "Персонаж уже существует в базе данных"
                );
            }

            var waifu = new Waifu
            {
                ShikiId = character.Id.ToString(),
                Name = character.Name ?? character.Russian ?? "Unknown",
                ImageUrl = character.Image?.Original ?? string.Empty,
                WhenAdded = DateTime.Now,
                LastOrder = DateTime.Now,
                OrderCount = 0,
                IsPrivated = false,
                Manga = character.Mangas.MinBy(e => e.Russian?.Length ?? int.MaxValue)?.Russian,
                Anime = character.Animes.MinBy(e => e.Russian?.Length ?? int.MaxValue)?.Russian,
            };

            waifu = await waifuDbHelper.EnsureWaifuHaveImageIrl(waifu);
            waifu = await waifuDbHelper.EnsureMangaAndAnimeTitleExists(waifu);

            await dbContext.Waifus.AddAsync(waifu);
            await dbContext.SaveChangesAsync();

            return OperationResult<AddNewWaifuResponse>.Ok(
                new AddNewWaifuResponse { Waifu = waifu, HasError = false }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при добавлении вайфу");
            return OperationResult<AddNewWaifuResponse>.Fail(
                $"Ошибка при добавлении вайфу: {ex.Message}"
            );
        }
    }

    public async Task<TimeSpan> GetWaifuRollCoolDownAsync(
        CancellationToken cancellationToken = default
    )
    {
        var result = await cooldownConfiguration.GetCooldownAsync(
            RootStateKeys.WaifuRollType,
            cancellationToken
        );

        return result;
    }
}
