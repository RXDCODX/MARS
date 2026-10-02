using MARS.Shared.Clients;
using MARS.Shared.Models;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Services;

public class MikuCollectionService(IDbContextFactory<WaifuDbContext> factory)
{
    private const int SameItemGuaranteeThreshold = 5;
    private const int UniqueItemGuaranteeThreshold = 20;

    public async Task<MikuCollectionStats> RecordRollAsync(string twitchUserId, int mikuPageId)
    {
        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();

            var existing = await dbContext.UserMikuCollections.FirstOrDefaultAsync(c =>
                c.TwitchUserId == twitchUserId && c.MikuPageId == mikuPageId
            );

            var isNew = existing is null;

            if (existing is not null)
            {
                existing.Count++;
                existing.LastObtained = DateTime.Now;
                dbContext.UserMikuCollections.Update(existing);
            }
            else
            {
                dbContext.UserMikuCollections.Add(
                    new UserMikuCollection
                    {
                        TwitchUserId = twitchUserId,
                        MikuPageId = mikuPageId,
                        Count = 1,
                        FirstObtained = DateTime.Now,
                        LastObtained = DateTime.Now,
                    }
                );
            }

            await dbContext.SaveChangesAsync();

            var collectedCount = await dbContext
                .UserMikuCollections.Where(c => c.TwitchUserId == twitchUserId)
                .Select(c => c.MikuPageId)
                .Distinct()
                .CountAsync();

            var totalCount = await dbContext.MikuModules.CountAsync();

            var thisModuleCount = existing?.Count + 1 ?? 1;

            var guaranteeTriggered = false;
            int? guaranteedPageId = null;

            if (
                thisModuleCount >= SameItemGuaranteeThreshold
                && thisModuleCount % SameItemGuaranteeThreshold == 0
            )
            {
                guaranteedPageId = await FindNewModuleAsync(dbContext, twitchUserId);
                guaranteeTriggered = guaranteedPageId is not null;
            }
            else if (
                collectedCount >= UniqueItemGuaranteeThreshold
                && collectedCount % UniqueItemGuaranteeThreshold == 0
            )
            {
                guaranteedPageId = await FindNewModuleAsync(dbContext, twitchUserId);
                guaranteeTriggered = guaranteedPageId is not null;
            }

            return new MikuCollectionStats
            {
                CollectedCount = collectedCount,
                TotalCount = totalCount,
                ThisModuleCount = thisModuleCount,
                IsNew = isNew,
                GuaranteeTriggered = guaranteeTriggered,
                GuaranteedPageId = guaranteedPageId,
            };
        }
        catch
        {
            return new MikuCollectionStats
            {
                CollectedCount = 0,
                TotalCount = 0,
                ThisModuleCount = 0,
                IsNew = false,
                GuaranteeTriggered = false,
                GuaranteedPageId = null,
            };
        }
    }

    public async Task<(int collected, int total)> GetUserCollectionStatsAsync(string twitchUserId)
    {
        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();

            var collected = await dbContext
                .UserMikuCollections.Where(c => c.TwitchUserId == twitchUserId)
                .Select(c => c.MikuPageId)
                .Distinct()
                .CountAsync();

            var total = await dbContext.MikuModules.CountAsync();

            return (collected, total);
        }
        catch
        {
            return (0, 0);
        }
    }

    /// <summary>
    /// Инвентарь пользователя: сколько разных модулей собрано, сколько есть
    /// всего и что именно собрано. Список отсортирован по количеству
    /// убыванию.
    /// </summary>
    public async Task<OperationResult<CollectionInventory>> GetInventoryAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        var result = OperationResult<CollectionInventory>.Fail("Стартовая ошибка чтения инвентаря");

        if (!string.IsNullOrWhiteSpace(twitchUserId))
        {
            try
            {
                await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

                var owned = await dbContext
                    .UserMikuCollections.AsNoTracking()
                    .Where(collection => collection.TwitchUserId == twitchUserId)
                    .Join(
                        dbContext.MikuModules.AsNoTracking(),
                        collection => collection.MikuPageId,
                        module => module.PageId,
                        (collection, module) => new { module.Title, collection.Count }
                    )
                    .OrderByDescending(item => item.Count)
                    .ThenBy(item => item.Title)
                    .ToListAsync(cancellationToken);

                var total = await dbContext.MikuModules.CountAsync(cancellationToken);

                var inventory = new CollectionInventory(
                    owned.Count,
                    total,
                    [.. owned.Select(item => new CollectionItem(item.Title, item.Count))]
                );

                result = OperationResult<CollectionInventory>.Ok(inventory);
            }
            catch (Exception ex)
            {
                result = OperationResult<CollectionInventory>.Fail(
                    $"Не удалось прочитать инвентарь: {ex.Message}"
                );
            }
        }
        else
        {
            result = OperationResult<CollectionInventory>.Fail("Twitch ID не передан");
        }

        return result;
    }

    private static async Task<int?> FindNewModuleAsync(
        WaifuDbContext dbContext,
        string twitchUserId
    )
    {
        var ownedIds = await dbContext
            .UserMikuCollections.Where(c => c.TwitchUserId == twitchUserId)
            .Select(c => c.MikuPageId)
            .Distinct()
            .ToListAsync();

        var newModule = await dbContext
            .MikuModules.Where(m => !ownedIds.Contains(m.PageId))
            .OrderBy(m => m.LastOrder)
            .FirstOrDefaultAsync();

        return newModule?.PageId;
    }
}

public record MikuCollectionStats
{
    public int CollectedCount { get; init; }
    public int TotalCount { get; init; }
    public int ThisModuleCount { get; init; }
    public bool IsNew { get; init; }
    public bool GuaranteeTriggered { get; init; }
    public int? GuaranteedPageId { get; init; }
}
