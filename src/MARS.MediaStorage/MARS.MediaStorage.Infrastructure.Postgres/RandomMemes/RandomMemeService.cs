using System.Data;
using MARS.MediaStorage.Application.Media;
using MARS.MediaStorage.Application.RandomMemes;
using MARS.MediaStorage.Domain.Entities;
using MARS.MediaStorage.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MARS.MediaStorage.Infrastructure.Postgres.RandomMemes;

public class RandomMemeService(
    IDbContextFactory<MediaStorageDbContext> contextFactory,
    ILogger<RandomMemeService> logger
) : IRandomMemeService
{
    #region MemeType CRUD Operations

    public async Task<IEnumerable<MemeType>> GetAllMemeTypesAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.RandomMemeType.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<MemeType?> GetMemeTypeByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        MemeType? result = null;

        if (id > 0)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            result = await context
                .RandomMemeType.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        return result;
    }

    public async Task<MemeType> CreateMemeTypeAsync(
        MemeType memeType,
        CancellationToken cancellationToken = default
    )
    {
        MemeType result = new() { Name = string.Empty, FolderPath = string.Empty };

        if (
            !string.IsNullOrWhiteSpace(memeType.Name)
            && !string.IsNullOrWhiteSpace(memeType.FolderPath)
        )
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var entity = new MemeType { Name = memeType.Name, FolderPath = memeType.FolderPath };

            context.RandomMemeType.Add(entity);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Created new MemeType: {Name} with ID: {Id}",
                entity.Name,
                entity.Id
            );
            result = entity;
        }

        return result;
    }

    public async Task<MemeType> UpdateMemeTypeAsync(
        MemeType memeType,
        CancellationToken cancellationToken = default
    )
    {
        MemeType result = new() { Name = string.Empty, FolderPath = string.Empty };

        if (
            memeType.Id > 0
            && !string.IsNullOrWhiteSpace(memeType.Name)
            && !string.IsNullOrWhiteSpace(memeType.FolderPath)
        )
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var existingEntity = await context.RandomMemeType.FirstOrDefaultAsync(
                x => x.Id == memeType.Id,
                cancellationToken
            );

            if (existingEntity != null)
            {
                existingEntity.Name = memeType.Name;
                existingEntity.FolderPath = memeType.FolderPath;

                await context.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Updated MemeType with ID: {Id}", memeType.Id);
                result = existingEntity;
            }
        }

        return result;
    }

    public async Task<bool> DeleteMemeTypeAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var result = false;

        if (id > 0)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var entity = await context.RandomMemeType.FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken
            );

            if (entity != null)
            {
                var hasOrders = await context.RandomMemeOrder.AnyAsync(
                    x => x.MemeTypeId == id,
                    cancellationToken
                );

                if (!hasOrders)
                {
                    context.RandomMemeType.Remove(entity);
                    await context.SaveChangesAsync(cancellationToken);

                    logger.LogInformation("Deleted MemeType with ID: {Id}", id);
                    result = true;
                }
            }
        }

        return result;
    }

    #endregion

    #region MemeOrder CRUD Operations

    public async Task<IEnumerable<MemeOrder>> GetAllMemeOrdersAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context
            .RandomMemeOrder.AsNoTracking()
            .Include(x => x.Type)
            .OrderBy(x => x.MemeTypeId)
            .ThenBy(x => x.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task<MemeOrder?> GetMemeOrderByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        MemeOrder? result = null;

        if (id != Guid.Empty)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            result = await context
                .RandomMemeOrder.AsNoTracking()
                .Include(x => x.Type)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        return result;
    }

    public async Task<IEnumerable<MemeOrder>> GetMemeOrdersByTypeAsync(
        int typeId,
        CancellationToken cancellationToken = default
    )
    {
        IEnumerable<MemeOrder> result = [];

        if (typeId > 0)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            result = await context
                .RandomMemeOrder.AsNoTracking()
                .Include(x => x.Type)
                .Where(x => x.MemeTypeId == typeId)
                .OrderBy(x => x.Order)
                .ToListAsync(cancellationToken);
        }

        return result;
    }

    public async Task<MemeOrder> CreateMemeOrderAsync(
        MemeOrder memeOrder,
        CancellationToken cancellationToken = default
    )
    {
        MemeOrder result = new() { FilePath = string.Empty };

        if (!string.IsNullOrWhiteSpace(memeOrder.FilePath) && memeOrder.MemeTypeId > 0)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var maxOrder =
                await context
                    .RandomMemeOrder.Where(x => x.MemeTypeId == memeOrder.MemeTypeId)
                    .MaxAsync(x => (int?)x.Order, cancellationToken)
                ?? 0;

            var entity = new MemeOrder
            {
                FilePath = memeOrder.FilePath,
                MemeTypeId = memeOrder.MemeTypeId,
                Order = maxOrder + 1,
            };

            context.RandomMemeOrder.Add(entity);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Created new MemeOrder: {FilePath} with Order: {Order}",
                entity.FilePath,
                entity.Order
            );
            result = entity;
        }

        return result;
    }

    public async Task<MemeOrder> UpdateMemeOrderAsync(
        MemeOrder memeOrder,
        CancellationToken cancellationToken = default
    )
    {
        MemeOrder result = new() { FilePath = string.Empty };

        if (
            memeOrder.Id != Guid.Empty
            && !string.IsNullOrWhiteSpace(memeOrder.FilePath)
            && memeOrder.MemeTypeId > 0
        )
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var existingEntity = await context.RandomMemeOrder.FirstOrDefaultAsync(
                x => x.Id == memeOrder.Id,
                cancellationToken
            );

            if (existingEntity != null)
            {
                existingEntity.FilePath = memeOrder.FilePath;
                existingEntity.MemeTypeId = memeOrder.MemeTypeId;
                existingEntity.Order = memeOrder.Order;

                await context.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Updated MemeOrder with ID: {Id}", memeOrder.Id);
                result = existingEntity;
            }
        }

        return result;
    }

    public async Task<bool> DeleteMemeOrderAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var result = false;

        if (id != Guid.Empty)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var entity = await context.RandomMemeOrder.FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken
            );

            if (entity != null)
            {
                context.RandomMemeOrder.Remove(entity);
                await context.SaveChangesAsync(cancellationToken);

                await ReorderMemeOrdersAsync(entity.MemeTypeId ?? 0, cancellationToken);

                logger.LogInformation("Deleted MemeOrder with ID: {Id}", id);
                result = true;
            }
        }

        return result;
    }

    #endregion

    #region Additional Operations

    /// <summary>
    /// Забирает первый элемент очереди указанной папки и переносит его в конец.
    /// Сериализуемая транзакция не позволяет двум одновременным погашениям
    /// выбрать одну и ту же запись из начала очереди.
    /// </summary>
    public async Task<MemeOrder?> GetNextMemeOrderAsync(
        string folderPath,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedFolderPath = MediaPath.Normalize(folderPath);
        if (string.IsNullOrWhiteSpace(normalizedFolderPath))
        {
            return null;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken
        );

        var memeTypes = await context.RandomMemeType.AsNoTracking().ToListAsync(cancellationToken);
        var memeType = memeTypes.FirstOrDefault(type =>
            string.Equals(
                MediaPath.Normalize(type.FolderPath),
                normalizedFolderPath,
                StringComparison.OrdinalIgnoreCase
            )
        );

        if (memeType is null)
        {
            return null;
        }

        var nextOrder = await context
            .RandomMemeOrder.Where(order => order.MemeTypeId == memeType.Id)
            .OrderBy(order => order.Order)
            .FirstOrDefaultAsync(cancellationToken);

        if (nextOrder is null)
        {
            return null;
        }

        var maxOrder =
            await context
                .RandomMemeOrder.Where(order => order.MemeTypeId == memeType.Id)
                .MaxAsync(order => (int?)order.Order, cancellationToken)
            ?? nextOrder.Order;

        // Сначала освобождаем позицию 1, чтобы сдвиг остальных строк не пересёкся
        // с выбранной записью даже при уникальном индексе очереди.
        await context
            .RandomMemeOrder.Where(order => order.Id == nextOrder.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(order => order.Order, -1),
                cancellationToken
            );

        await context
            .RandomMemeOrder.Where(order =>
                order.MemeTypeId == memeType.Id && order.Id != nextOrder.Id
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(order => order.Order, order => order.Order - 1),
                cancellationToken
            );

        context.Entry(nextOrder).Property(order => order.Order).OriginalValue = -1;
        nextOrder.Order = maxOrder;
        nextOrder.Type = memeType;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogDebug(
            "Selected next meme {FilePath} from {FolderPath}; new queue position {Order}",
            nextOrder.FilePath,
            normalizedFolderPath,
            nextOrder.Order
        );

        return nextOrder;
    }

    public async Task<MemeOrder?> GetRandomMemeAsync(
        int? typeId = null,
        CancellationToken cancellationToken = default
    )
    {
        MemeOrder? result = null;

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.RandomMemeOrder.AsNoTracking();

        if (typeId is > 0)
        {
            query = query.Where(x => x.MemeTypeId == typeId.Value);
        }

        var count = await query.CountAsync(cancellationToken);
        if (count > 0)
        {
            var randomIndex = Random.Shared.Next(count);
            result = await query
                .Include(x => x.Type)
                .Skip(randomIndex)
                .FirstAsync(cancellationToken);
        }

        return result;
    }

    public async Task<int> GetMemeOrderCountAsync(
        int? typeId = null,
        CancellationToken cancellationToken = default
    )
    {
        var result = 0;

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.RandomMemeOrder.AsNoTracking();

        if (typeId is > 0)
        {
            query = query.Where(x => x.MemeTypeId == typeId.Value);
        }

        result = await query.CountAsync(cancellationToken);

        return result;
    }

    public async Task ReorderMemeOrdersAsync(
        int typeId,
        CancellationToken cancellationToken = default
    )
    {
        if (typeId > 0)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var orders = await context
                .RandomMemeOrder.Where(x => x.MemeTypeId == typeId)
                .OrderBy(x => x.Order)
                .ToListAsync(cancellationToken);

            for (var i = 0; i < orders.Count; i++)
            {
                orders[i].Order = i + 1;
            }

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Reordered {Count} MemeOrders for type {TypeId}",
                orders.Count,
                typeId
            );
        }
    }

    #endregion
}
