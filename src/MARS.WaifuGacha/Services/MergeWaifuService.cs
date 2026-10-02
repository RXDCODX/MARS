using System.Collections.Concurrent;
using MARS.Shared.Models;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Services;

public class MergeWaifuService(
    ILogger<MergeWaifuService> logger,
    IDbContextFactory<WaifuDbContext> factory,
    WaifuRollService waifuRollService,
    WaifuRollEnsurenceService waifuDbHelper
)
{
    private class SemaphoreWrapper
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public int UseCount;
    }

    private readonly ConcurrentDictionary<string, SemaphoreWrapper> _hostSemaphores = new();

    private SemaphoreSlim GetOrCreateSemaphore(string userId)
    {
        var wrapper = _hostSemaphores.GetOrAdd(userId, _ => new SemaphoreWrapper());
        Interlocked.Increment(ref wrapper.UseCount);
        return wrapper.Semaphore;
    }

    private void ReleaseSemaphore(string userId, SemaphoreSlim semaphore)
    {
        semaphore.Release();

        if (_hostSemaphores.TryGetValue(userId, out var wrapper))
        {
            var count = Interlocked.Decrement(ref wrapper.UseCount);

            if (count == 0)
            {
                if (_hostSemaphores.TryRemove(userId, out var removedWrapper))
                {
                    removedWrapper.Semaphore.Dispose();
                }
            }
        }
    }

    public async Task<OperationResult<MergeWaifuResult>> MergeWaifuAsync(string userId)
    {
        var semaphore = GetOrCreateSemaphore(userId);
        await semaphore.WaitAsync();

        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();

            var host = await dbContext
                .Husbands.Include(h => h.HusbandCoolDown)
                .FirstOrDefaultAsync(h => h.TwitchId == userId);

            if (host is null)
            {
                host = new Husband
                {
                    TwitchId = userId,
                    HusbandCoolDown = new HusbandCoolDown { HusbandId = userId },
                    HusbandGreetings = new HusbandAutoHello { HusbandId = userId },
                };

                dbContext.Husbands.Add(host);
                await dbContext.SaveChangesAsync();

                return OperationResult<MergeWaifuResult>.Fail("Ты новенький, тебе пока нельзя!");
            }

            if (!host.IsPrivated)
            {
                var waifu = await dbContext.Waifus.FindAsync(host.WaifuRollId);
                if (waifu is { IsPrivated: false })
                {
                    var isMerged = await waifuRollService.MergeTheWaifu(host, waifu);

                    if (isMerged)
                    {
                        if (string.IsNullOrWhiteSpace(waifu.ImageUrl))
                        {
                            waifu = await waifuDbHelper.EnsureWaifuHaveImageIrl(waifu);
                        }

                        waifu = await waifuDbHelper.EnsureMangaAndAnimeTitleExists(waifu);

                        waifu.IsMerged = true;
                        dbContext.Waifus.Update(waifu);
                        await dbContext.SaveChangesAsync();

                        // Ссылка на картинку приходит абсолютной из MARS.Shikimori: склеивать её с

                        return OperationResult<MergeWaifuResult>.Ok(
                            new MergeWaifuResult
                            {
                                Waifu = waifu,
                                Host = host,
                                IsNewMarriage = true,
                            }
                        );
                    }
                }
                else if (waifu is { IsPrivated: true })
                {
                    return OperationResult<MergeWaifuResult>.Fail("Твоя любовь уже занята :-(");
                }
                else
                {
                    return OperationResult<MergeWaifuResult>.Fail(
                        "Не удалось найти твою любовь в бд :-("
                    );
                }
            }
            else
            {
                var waifu = await dbContext.Waifus.FindAsync(host.WaifuBrideId);

                if (waifu is { IsPrivated: true })
                {
                    if (string.IsNullOrWhiteSpace(waifu.ImageUrl))
                    {
                        waifu = await waifuDbHelper.EnsureWaifuHaveImageIrl(waifu);
                    }

                    waifu = await waifuDbHelper.EnsureMangaAndAnimeTitleExists(waifu);

                    dbContext.Waifus.Update(waifu);
                    await dbContext.SaveChangesAsync();

                    // Ссылка на картинку приходит абсолютной из MARS.Shikimori: склеивать её с

                    return OperationResult<MergeWaifuResult>.Ok(
                        new MergeWaifuResult
                        {
                            Waifu = waifu,
                            Host = host,
                            IsNewMarriage = false,
                        }
                    );
                }

                return OperationResult<MergeWaifuResult>.Fail(
                    "Неправильный айдишник у женатого мужика: " + host.TwitchId
                );
            }

            return OperationResult<MergeWaifuResult>.Fail("Не удалось обработать запрос");
        }
        finally
        {
            ReleaseSemaphore(userId, semaphore);
        }
    }

    public async Task<OperationResult<UnmergeResult>> UnmergeAsync(string nickname)
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        var host = await dbContext
            .Husbands.Include(e => e.HusbandCoolDown)
            .FirstOrDefaultAsync(e =>
                e.TwitchId != null && EF.Functions.ILike(e.TwitchId, $"%{nickname}%")
            );

        if (host is { IsPrivated: true })
        {
            var waifu = await dbContext.Waifus.SingleOrDefaultAsync(e =>
                e.ShikiId == host.WaifuBrideId
            );

            if (waifu is { IsPrivated: true })
            {
                waifu.IsPrivated = false;
                host.WaifuBrideId = null;
                host.IsPrivated = false;
                host.WhenPrivated = null;

                dbContext.Waifus.Update(waifu);
                dbContext.Husbands.Update(host);
                await dbContext.SaveChangesAsync();

                return OperationResult<UnmergeResult>.Ok(
                    new UnmergeResult { Waifu = waifu, Host = host }
                );
            }

            return OperationResult<UnmergeResult>.Fail("Вайфу не найдена или не в браке");
        }

        return OperationResult<UnmergeResult>.Fail("Муж не найден или не в браке");
    }

    public async Task<OperationResult<UnmergeResult>> UnmergeByIdAsync(int id)
    {
        await using var dbContext = await factory.CreateDbContextAsync();

        var host = await dbContext.Husbands.SingleOrDefaultAsync(e => e.TwitchId == id.ToString());

        if (host is { IsPrivated: true })
        {
            var waifu = await dbContext.Waifus.SingleOrDefaultAsync(e =>
                e.ShikiId == host.WaifuBrideId
            );

            if (waifu is { IsPrivated: true })
            {
                waifu.IsPrivated = false;
                host.WaifuBrideId = null;
                host.IsPrivated = false;
                host.WhenPrivated = null;

                dbContext.Waifus.Update(waifu);
                dbContext.Husbands.Update(host);
                await dbContext.SaveChangesAsync();

                return OperationResult<UnmergeResult>.Ok(
                    new UnmergeResult { Waifu = waifu, Host = host }
                );
            }

            return OperationResult<UnmergeResult>.Fail("Вайфу не найдена или не в браке");
        }

        return OperationResult<UnmergeResult>.Fail("Муж не найден или не в браке");
    }

    public void Dispose()
    {
        foreach (var wrapper in _hostSemaphores.Values)
        {
            wrapper?.Semaphore.Dispose();
        }
        _hostSemaphores.Clear();
    }
}

public class MergeWaifuResult
{
    public Waifu? Waifu { get; set; }
    public Husband? Host { get; set; }
    public bool IsNewMarriage { get; set; }
}

public class UnmergeResult
{
    public Waifu? Waifu { get; set; }
    public Husband? Host { get; set; }
}
