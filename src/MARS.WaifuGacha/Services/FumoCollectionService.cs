using MARS.Shared.Clients;
using MARS.Shared.Models;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Services;

public class FumoCollectionService(IDbContextFactory<WaifuDbContext> factory)
{
    private const int SameItemGuaranteeThreshold = 5;
    private const int UniqueItemGuaranteeThreshold = 20;

    public async Task<FumoCollectionStats> RecordRollAsync(string twitchUserId, int fumoMfcId)
    {
        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();

            var existing = await dbContext.UserFumoCollections.FirstOrDefaultAsync(c =>
                c.TwitchUserId == twitchUserId && c.FumoMfcId == fumoMfcId
            );

            var isNew = existing is null;

            if (existing is not null)
            {
                existing.Count++;
                existing.LastObtained = DateTime.Now;
                dbContext.UserFumoCollections.Update(existing);
            }
            else
            {
                dbContext.UserFumoCollections.Add(
                    new UserFumoCollection
                    {
                        TwitchUserId = twitchUserId,
                        FumoMfcId = fumoMfcId,
                        Count = 1,
                        FirstObtained = DateTime.Now,
                        LastObtained = DateTime.Now,
                    }
                );
            }

            await dbContext.SaveChangesAsync();

            var collectedCount = await dbContext
                .UserFumoCollections.Where(c => c.TwitchUserId == twitchUserId)
                .Select(c => c.FumoMfcId)
                .Distinct()
                .CountAsync();

            var totalCount = await dbContext.Fumos.CountAsync();

            // existing уже увеличен на единицу выше, поэтому прибавлять ещё раз
            // нельзя: счётчик предметов уезжал бы на единицу и гарантия срабатывала
            // бы на пятом ролле вместо пятого предмета.
            var thisItemCount = existing?.Count ?? 1;

            var guaranteeTriggered = false;
            int? guaranteedItemId = null;

            if (
                thisItemCount >= SameItemGuaranteeThreshold
                && thisItemCount % SameItemGuaranteeThreshold == 0
            )
            {
                guaranteedItemId = await FindNewFumoAsync(dbContext, twitchUserId);
                guaranteeTriggered = guaranteedItemId is not null;
            }
            else if (
                collectedCount >= UniqueItemGuaranteeThreshold
                && collectedCount % UniqueItemGuaranteeThreshold == 0
            )
            {
                guaranteedItemId = await FindNewFumoAsync(dbContext, twitchUserId);
                guaranteeTriggered = guaranteedItemId is not null;
            }

            return new FumoCollectionStats
            {
                CollectedCount = collectedCount,
                TotalCount = totalCount,
                ThisItemCount = thisItemCount,
                IsNew = isNew,
                GuaranteeTriggered = guaranteeTriggered,
                GuaranteedItemId = guaranteedItemId,
            };
        }
        catch
        {
            return new FumoCollectionStats
            {
                CollectedCount = 0,
                TotalCount = 0,
                ThisItemCount = 0,
                IsNew = false,
                GuaranteeTriggered = false,
                GuaranteedItemId = null,
            };
        }
    }

    public async Task<(int collected, int total)> GetUserFumoCollectionStatsAsync(
        string twitchUserId
    )
    {
        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();

            var collected = await dbContext
                .UserFumoCollections.Where(c => c.TwitchUserId == twitchUserId)
                .Select(c => c.FumoMfcId)
                .Distinct()
                .CountAsync();

            var total = await dbContext.Fumos.CountAsync();

            return (collected, total);
        }
        catch
        {
            return (0, 0);
        }
    }

    /// <summary>
    /// Инвентарь пользователя: сколько разных фумо собрано, сколько есть всего
    /// и что именно собрано. Список отсортирован по количеству убыванию —
    /// так он читается как «что чаще всего выпадало».
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
                    .UserFumoCollections.AsNoTracking()
                    .Where(collection => collection.TwitchUserId == twitchUserId)
                    .Join(
                        dbContext.Fumos.AsNoTracking(),
                        collection => collection.FumoMfcId,
                        fumo => fumo.MfcId,
                        (collection, fumo) => new { fumo.Name, collection.Count }
                    )
                    .OrderByDescending(item => item.Count)
                    .ThenBy(item => item.Name)
                    .ToListAsync(cancellationToken);

                var total = await dbContext.Fumos.CountAsync(cancellationToken);

                var inventory = new CollectionInventory(
                    owned.Count,
                    total,
                    [.. owned.Select(item => new CollectionItem(item.Name, item.Count))]
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

    private static async Task<int?> FindNewFumoAsync(WaifuDbContext dbContext, string twitchUserId)
    {
        var ownedIds = await dbContext
            .UserFumoCollections.Where(c => c.TwitchUserId == twitchUserId)
            .Select(c => c.FumoMfcId)
            .Distinct()
            .ToListAsync();

        var newFumo = await dbContext
            .Fumos.Where(f => !ownedIds.Contains(f.MfcId))
            .OrderBy(f => f.LastOrder)
            .FirstOrDefaultAsync();

        return newFumo?.MfcId;
    }
}

public record FumoCollectionStats
{
    public int CollectedCount { get; init; }
    public int TotalCount { get; init; }
    public int ThisItemCount { get; init; }
    public bool IsNew { get; init; }
    public bool GuaranteeTriggered { get; init; }
    public int? GuaranteedItemId { get; init; }
}
