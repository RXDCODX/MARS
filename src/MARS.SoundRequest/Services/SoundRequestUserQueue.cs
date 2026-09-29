using MARS.SoundRequest.Data;
using MARS.SoundRequest.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace MARS.SoundRequest.Services;

public class SoundRequestUserQueue(
    IDbContextFactory<MediaDbContext> contextFactory,
    IHostApplicationLifetime lifetime,
    StateManager? stateManager = null
)
{
    private readonly CancellationToken _cancellationToken = lifetime.ApplicationStopping;

    public async Task<QueueItem> AddToQueueAsync(
        BaseTrackInfo track,
        string requestedByTwitchId,
        DateTime? requestedAt = null
    )
    {
        QueueItem result = null!;

        await using var dbContext = await contextFactory.CreateDbContextAsync(_cancellationToken);

        if (!string.IsNullOrWhiteSpace(requestedByTwitchId))
        {
            BaseTrackInfo? existingTrack = null;

            if (!string.IsNullOrWhiteSpace(track.VideoId))
            {
                existingTrack = await dbContext
                    .Tracks.AsNoTracking()
                    .FirstOrDefaultAsync(
                        t => t.VideoId == track.VideoId,
                        cancellationToken: _cancellationToken
                    );
            }

            existingTrack ??= await dbContext
                .Tracks.AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.Url == track.Url,
                    cancellationToken: _cancellationToken
                );

            Guid trackId;
            if (existingTrack != null)
            {
                trackId = existingTrack.Id;
                existingTrack.TrackName = track.TrackName;
                existingTrack.Authors = track.Authors;
                existingTrack.Duration = track.Duration;
                existingTrack.ArtworkUrl = track.ArtworkUrl;
                existingTrack.VideoId = track.VideoId;
                existingTrack.UpdatedAt = DateTime.Now;
                dbContext.Tracks.Update(existingTrack);
            }
            else
            {
                dbContext.Tracks.Add(track);
                await dbContext.SaveChangesAsync(_cancellationToken);
                trackId = track.Id;
            }

            var isQueueItemsExists = await dbContext
                .QueueItems.Where(e => e.QueueOrder >= 0)
                .AsNoTracking()
                .AnyAsync(cancellationToken: _cancellationToken);

            var maxOrder = isQueueItemsExists
                ? await dbContext
                    .QueueItems.AsNoTracking()
                    .Where(e => e.QueueOrder >= 0)
                    .MaxAsync(e => e.QueueOrder, cancellationToken: _cancellationToken)
                : -1;

            var queueItem = new QueueItem
            {
                TrackId = trackId,
                Track = existingTrack ?? track,
                QueueOrder = maxOrder + 1,
                RequestedByTwitchId = requestedByTwitchId,
                RequestedAt = requestedAt ?? DateTime.Now,
            };

            dbContext.QueueItems.Add(queueItem);
            await dbContext.SaveChangesAsync(_cancellationToken);

            result = queueItem;
        }

        return result;
    }

    public async Task RemoveFromQueueAsync(Guid queueItemId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(_cancellationToken);

        var queueItemToRemove = await dbContext.QueueItems.FindAsync(
            [queueItemId],
            cancellationToken: _cancellationToken
        );

        if (queueItemToRemove != null)
        {
            if (stateManager != null)
            {
                var currentState = await stateManager.GetStateAsync();

                if (currentState.CurrentQueueItemId == queueItemToRemove.Id)
                {
                    await stateManager.StopPlaybackAsync(notify: true);
                }
            }

            var removedOrder = queueItemToRemove.QueueOrder;

            dbContext.QueueItems.Remove(queueItemToRemove);
            await dbContext.SaveChangesAsync(_cancellationToken);

            try
            {
                await dbContext
                    .QueueItems.Where(qi => qi.QueueOrder > removedOrder)
                    .ExecuteUpdateAsync(
                        e => e.SetProperty(qi => qi.QueueOrder, qi => qi.QueueOrder - 1),
                        cancellationToken: _cancellationToken
                    );
            }
            catch (InvalidOperationException)
            {
                var affectedItems = await dbContext
                    .QueueItems.Where(qi => qi.QueueOrder > removedOrder)
                    .ToListAsync(cancellationToken: _cancellationToken);

                foreach (var affectedItem in affectedItems)
                {
                    affectedItem.QueueOrder -= 1;
                }
            }

            await dbContext.SaveChangesAsync(_cancellationToken);
        }
    }

    public async Task<int> ClearQueueAsync()
    {
        var result = 0;

        await using var dbContext = await contextFactory.CreateDbContextAsync(_cancellationToken);

        var queueItemsQuery = dbContext.QueueItems.Where(qi => qi.QueueOrder >= 0);
        var hasQueueItems = await queueItemsQuery.AnyAsync(cancellationToken: _cancellationToken);

        if (hasQueueItems && stateManager != null)
        {
            await stateManager.StopPlaybackAsync(notify: true);
        }

        try
        {
            result = await queueItemsQuery.ExecuteDeleteAsync(
                cancellationToken: _cancellationToken
            );
        }
        catch (InvalidOperationException)
        {
            var queueItems = await queueItemsQuery.ToListAsync(
                cancellationToken: _cancellationToken
            );

            result = queueItems.Count;
            dbContext.QueueItems.RemoveRange(queueItems);
            await dbContext.SaveChangesAsync(_cancellationToken);
        }

        return result;
    }

    public async Task<List<QueueItem>> GetQueueAsync()
    {
        List<QueueItem> result = [];

        await using var dbContext = await contextFactory.CreateDbContextAsync(_cancellationToken);

        result = await dbContext
            .QueueItems.AsNoTracking()
            .Include(qi => qi.Track)
            .Where(qi => qi.QueueOrder >= 0)
            .OrderBy(qi => qi.QueueOrder)
            .ToListAsync(cancellationToken: _cancellationToken);

        return result;
    }

    public async Task<QueueItem?> GetCurrentQueueItemAsync()
    {
        QueueItem? result = null;

        await using var dbContext = await contextFactory.CreateDbContextAsync(_cancellationToken);

        result = await dbContext
            .QueueItems.AsNoTracking()
            .Include(qi => qi.Track)
            .Where(qi => qi.QueueOrder == 0)
            .FirstOrDefaultAsync(cancellationToken: _cancellationToken);

        return result;
    }

    public async Task<QueueItem?> GetNextQueueItemAsync()
    {
        QueueItem? result = null;

        await using var dbContext = await contextFactory.CreateDbContextAsync(_cancellationToken);

        result = await dbContext
            .QueueItems.AsNoTracking()
            .Include(qi => qi.Track)
            .Where(qi => qi.QueueOrder == 1)
            .FirstOrDefaultAsync(cancellationToken: _cancellationToken);

        return result;
    }

    public async Task<int> GetQueueCountAsync()
    {
        var result = 0;

        await using var dbContext = await contextFactory.CreateDbContextAsync(_cancellationToken);

        result = await dbContext.QueueItems.CountAsync(
            qi => qi.QueueOrder >= 0,
            cancellationToken: _cancellationToken
        );

        return result;
    }

    public async Task<QueueItem?> GetQueueItemByIdAsync(Guid queueItemId)
    {
        QueueItem? result = null;

        await using var dbContext = await contextFactory.CreateDbContextAsync(_cancellationToken);

        result = await dbContext
            .QueueItems.AsNoTracking()
            .Include(qi => qi.Track)
            .FirstOrDefaultAsync(qi => qi.Id == queueItemId, cancellationToken: _cancellationToken);

        return result;
    }

    public async Task<QueueItem> AddToQueueFrontAsync(
        BaseTrackInfo track,
        string requestedByTwitchId
    )
    {
        QueueItem result = null!;

        await using var dbContext = await contextFactory.CreateDbContextAsync(_cancellationToken);

        if (!string.IsNullOrWhiteSpace(requestedByTwitchId))
        {
            try
            {
                await dbContext
                    .QueueItems.Where(qi => qi.QueueOrder >= 1)
                    .ExecuteUpdateAsync(
                        e => e.SetProperty(qi => qi.QueueOrder, qi => qi.QueueOrder + 1),
                        cancellationToken: _cancellationToken
                    );
            }
            catch (InvalidOperationException)
            {
                var queueItems = await dbContext
                    .QueueItems.Where(qi => qi.QueueOrder >= 1)
                    .ToListAsync(cancellationToken: _cancellationToken);

                foreach (var item in queueItems)
                {
                    item.QueueOrder += 1;
                }
            }

            BaseTrackInfo? existingTrack = null;

            if (!string.IsNullOrWhiteSpace(track.VideoId))
            {
                existingTrack = await dbContext
                    .Tracks.AsNoTracking()
                    .FirstOrDefaultAsync(
                        t => t.VideoId == track.VideoId,
                        cancellationToken: _cancellationToken
                    );
            }

            existingTrack ??= await dbContext
                .Tracks.AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.Url == track.Url,
                    cancellationToken: _cancellationToken
                );

            Guid trackId;
            if (existingTrack != null)
            {
                trackId = existingTrack.Id;
                existingTrack.TrackName = track.TrackName;
                existingTrack.Authors = track.Authors;
                existingTrack.Duration = track.Duration;
                existingTrack.ArtworkUrl = track.ArtworkUrl;
                existingTrack.VideoId = track.VideoId;
                existingTrack.UpdatedAt = DateTime.Now;
                dbContext.Tracks.Update(existingTrack);
            }
            else
            {
                dbContext.Tracks.Add(track);
                await dbContext.SaveChangesAsync(_cancellationToken);
                trackId = track.Id;
            }

            var queueItem = new QueueItem
            {
                TrackId = trackId,
                Track = existingTrack ?? track,
                QueueOrder = 1,
                RequestedByTwitchId = requestedByTwitchId,
                RequestedAt = DateTime.Now,
            };

            dbContext.QueueItems.Add(queueItem);
            await dbContext.SaveChangesAsync(_cancellationToken);

            result = queueItem;
        }

        return result;
    }

    public async Task<List<QueueItem>> GetUserQueueItemsAsync(string twitchId)
    {
        List<QueueItem> result = [];

        await using var dbContext = await contextFactory.CreateDbContextAsync(_cancellationToken);

        if (!string.IsNullOrWhiteSpace(twitchId))
        {
            result = await dbContext
                .QueueItems.AsNoTracking()
                .Include(qi => qi.Track)
                .Where(qi => qi.QueueOrder >= 0 && qi.RequestedByTwitchId == twitchId)
                .OrderBy(qi => qi.QueueOrder)
                .ToListAsync(cancellationToken: _cancellationToken);
        }

        return result;
    }

    public async Task<QueueItem?> ShiftQueueAndGetCurrentAsync()
    {
        QueueItem? result = null;

        await using var dbContext = await contextFactory.CreateDbContextAsync(_cancellationToken);

        try
        {
            await dbContext.QueueItems.ExecuteUpdateAsync(
                e => e.SetProperty(qi => qi.QueueOrder, qi => qi.QueueOrder - 1),
                cancellationToken: _cancellationToken
            );
        }
        catch (InvalidOperationException)
        {
            var queueItems = await dbContext.QueueItems.ToListAsync(
                cancellationToken: _cancellationToken
            );

            foreach (var queueItem in queueItems)
            {
                queueItem.QueueOrder -= 1;
            }

            await dbContext.SaveChangesAsync(_cancellationToken);
        }

        result = await dbContext
            .QueueItems.AsNoTracking()
            .Include(qi => qi.Track)
            .Where(qi => qi.QueueOrder == 0)
            .FirstOrDefaultAsync(cancellationToken: _cancellationToken);

        return result;
    }

    public async Task<QueueItem?> MoveToFrontAndPlayAsync(Guid queueItemId)
    {
        QueueItem? result = null;

        await using var dbContext = await contextFactory.CreateDbContextAsync(_cancellationToken);

        var itemToMove = await dbContext
            .QueueItems.Include(qi => qi.Track)
            .FirstOrDefaultAsync(qi => qi.Id == queueItemId, cancellationToken: _cancellationToken);

        if (itemToMove != null && itemToMove.QueueOrder != 0)
        {
            var itemQueueOrder = itemToMove.QueueOrder;

            if (itemQueueOrder > 0)
            {
                try
                {
                    await dbContext
                        .QueueItems.Where(qi => qi.QueueOrder < 0)
                        .ExecuteUpdateAsync(
                            e => e.SetProperty(qi => qi.QueueOrder, qi => qi.QueueOrder - 1),
                            cancellationToken: _cancellationToken
                        );
                }
                catch (InvalidOperationException)
                {
                    var historyItems = await dbContext
                        .QueueItems.Where(qi => qi.QueueOrder < 0)
                        .ToListAsync(cancellationToken: _cancellationToken);

                    foreach (var item in historyItems)
                    {
                        item.QueueOrder -= 1;
                    }
                }

                var currentItem = await dbContext.QueueItems.FirstOrDefaultAsync(
                    qi => qi.QueueOrder == 0,
                    cancellationToken: _cancellationToken
                );

                currentItem?.QueueOrder = -1;

                try
                {
                    await dbContext
                        .QueueItems.Where(qi => qi.QueueOrder > itemQueueOrder)
                        .ExecuteUpdateAsync(
                            e => e.SetProperty(qi => qi.QueueOrder, qi => qi.QueueOrder - 1),
                            cancellationToken: _cancellationToken
                        );
                }
                catch (InvalidOperationException)
                {
                    var affectedItems = await dbContext
                        .QueueItems.Where(qi => qi.QueueOrder > itemQueueOrder)
                        .ToListAsync(cancellationToken: _cancellationToken);

                    foreach (var item in affectedItems)
                    {
                        item.QueueOrder -= 1;
                    }
                }
            }
            else
            {
                var currentItem = await dbContext.QueueItems.FirstOrDefaultAsync(
                    qi => qi.QueueOrder == 0,
                    cancellationToken: _cancellationToken
                );

                currentItem?.QueueOrder = 1;

                try
                {
                    await dbContext
                        .QueueItems.Where(qi => qi.QueueOrder >= 1)
                        .ExecuteUpdateAsync(
                            e => e.SetProperty(qi => qi.QueueOrder, qi => qi.QueueOrder + 1),
                            cancellationToken: _cancellationToken
                        );
                }
                catch (InvalidOperationException)
                {
                    var affectedItems = await dbContext
                        .QueueItems.Where(qi => qi.QueueOrder >= 1)
                        .ToListAsync(cancellationToken: _cancellationToken);

                    foreach (var item in affectedItems)
                    {
                        item.QueueOrder += 1;
                    }
                }
            }

            itemToMove.QueueOrder = 0;

            await dbContext.SaveChangesAsync(_cancellationToken);

            result = await dbContext
                .QueueItems.AsNoTracking()
                .Include(qi => qi.Track)
                .FirstOrDefaultAsync(
                    qi => qi.Id == queueItemId,
                    cancellationToken: _cancellationToken
                );
        }

        return result;
    }

    public async Task<QueueItem?> MoveQueueItemToPositionAsync(Guid queueItemId, int newPosition)
    {
        var state = stateManager?.GetState().State;
        if (
            newPosition == 0
            && (
                state == PlaybackState.Playing
                || state == PlaybackState.SwitchingTrack
                || state == PlaybackState.Paused
            )
        )
        {
            throw new Exception(
                "Ты не можешь менять текущий трек, вместо этого вызови другой метод для проигрывания своего трека!"
            );
        }

        QueueItem? result = null;

        await using var dbContext = await contextFactory.CreateDbContextAsync(_cancellationToken);

        var itemToMove = await dbContext
            .QueueItems.Include(qi => qi.Track)
            .FirstOrDefaultAsync(qi => qi.Id == queueItemId, cancellationToken: _cancellationToken);

        if (itemToMove is null)
        {
            return null;
        }

        if (newPosition < 0)
        {
            return null;
        }

        var hasCurrentQueueItem = await dbContext.QueueItems.AnyAsync(
            qi => qi.QueueOrder == 0 && qi.Id != queueItemId,
            cancellationToken: _cancellationToken
        );

        var queueCount = await dbContext.QueueItems.CountAsync(
            qi => qi.QueueOrder >= 0,
            cancellationToken: _cancellationToken
        );

        var oldPos = itemToMove.QueueOrder;
        var maxPos = oldPos > 0 ? Math.Max(queueCount - 1, 0) : queueCount;

        var targetPos = Math.Min(newPosition, maxPos);
        if (oldPos == targetPos)
        {
            result = itemToMove;
            return result;
        }

        if (oldPos >= 0)
        {
            if (targetPos < oldPos)
            {
                try
                {
                    await dbContext
                        .QueueItems.Where(qi =>
                            qi.QueueOrder >= targetPos && qi.QueueOrder < oldPos
                        )
                        .ExecuteUpdateAsync(
                            e => e.SetProperty(qi => qi.QueueOrder, qi => qi.QueueOrder + 1),
                            cancellationToken: _cancellationToken
                        );
                }
                catch (InvalidOperationException)
                {
                    var affected = await dbContext
                        .QueueItems.Where(qi =>
                            qi.QueueOrder >= targetPos && qi.QueueOrder < oldPos
                        )
                        .ToListAsync(cancellationToken: _cancellationToken);

                    foreach (var a in affected)
                    {
                        a.QueueOrder += 1;
                    }
                }
            }
            else
            {
                try
                {
                    await dbContext
                        .QueueItems.Where(qi =>
                            qi.QueueOrder > oldPos && qi.QueueOrder <= targetPos
                        )
                        .ExecuteUpdateAsync(
                            e => e.SetProperty(qi => qi.QueueOrder, qi => qi.QueueOrder - 1),
                            cancellationToken: _cancellationToken
                        );
                }
                catch (InvalidOperationException)
                {
                    var affected = await dbContext
                        .QueueItems.Where(qi =>
                            qi.QueueOrder > oldPos && qi.QueueOrder <= targetPos
                        )
                        .ToListAsync(cancellationToken: _cancellationToken);

                    foreach (var a in affected)
                    {
                        a.QueueOrder -= 1;
                    }
                }
            }
        }
        else
        {
            if (targetPos == 0 && hasCurrentQueueItem)
            {
                targetPos = 1;
            }

            try
            {
                await dbContext
                    .QueueItems.Where(qi => qi.QueueOrder < oldPos)
                    .ExecuteUpdateAsync(
                        e => e.SetProperty(qi => qi.QueueOrder, qi => qi.QueueOrder + 1),
                        cancellationToken: _cancellationToken
                    );
            }
            catch (InvalidOperationException)
            {
                var historyAffected = await dbContext
                    .QueueItems.Where(qi => qi.QueueOrder < oldPos)
                    .ToListAsync(cancellationToken: _cancellationToken);

                foreach (var a in historyAffected)
                {
                    a.QueueOrder += 1;
                }
            }

            try
            {
                await dbContext
                    .QueueItems.Where(qi => qi.QueueOrder >= targetPos)
                    .ExecuteUpdateAsync(
                        e => e.SetProperty(qi => qi.QueueOrder, qi => qi.QueueOrder + 1),
                        cancellationToken: _cancellationToken
                    );
            }
            catch (InvalidOperationException)
            {
                var queueAffected = await dbContext
                    .QueueItems.Where(qi => qi.QueueOrder >= targetPos)
                    .ToListAsync(cancellationToken: _cancellationToken);

                foreach (var a in queueAffected)
                {
                    a.QueueOrder += 1;
                }
            }
        }

        itemToMove.QueueOrder = targetPos;

        await dbContext.SaveChangesAsync(_cancellationToken);

        result = itemToMove;

        return result;
    }
}
