using MARS.CinemaQueue.Entities;
using MARS.CinemaQueue.Interfaces;

namespace MARS.CinemaQueue.Services;

public class CinemaQueueService(
    ICinemaQueueRepository repository,
    ILogger<CinemaQueueService> logger
) : ICinemaQueueService
{
    public async Task<IEnumerable<CinemaMediaItemDto>> GetAllMediaItemsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var items = await repository.GetAllAsync(cancellationToken);
        return items.Select(MapToDto);
    }

    public async Task<CinemaMediaItemDto?> GetMediaItemByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        var item = await repository.GetByIdAsync(id, cancellationToken);
        return item != null ? MapToDto(item) : null;
    }

    public async Task<CinemaMediaItemDto?> GetNextMediaItemAsync(
        CancellationToken cancellationToken = default
    )
    {
        var item = await repository.GetNextAsync(cancellationToken);
        return item != null ? MapToDto(item) : null;
    }

    public async Task<IEnumerable<CinemaMediaItemDto>> GetMediaItemsByStatusAsync(
        MediaStatus status,
        CancellationToken cancellationToken = default
    )
    {
        var items = await repository.GetByStatusAsync(status, cancellationToken);
        return items.Select(MapToDto);
    }

    public async Task<CinemaMediaItemDto> CreateMediaItemAsync(
        CreateMediaItemRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var mediaItem = new CinemaMediaItem
        {
            Title = request.Title,
            Description = request.Description,
            MediaUrl = request.MediaUrl,
            Priority = request.Priority,
            ScheduledFor = request.ScheduledFor,
            TwitchUserId = request.TwitchUserId,
            Notes = request.Notes,
            Status = MediaStatus.Pending,
            IsNext = false,
            CreatedAt = DateTime.Now,
            LastModified = DateTime.Now,
        };

        var createdItem = await repository.CreateAsync(mediaItem, cancellationToken);
        logger.LogInformation(
            "Created media item: {Title} with ID: {Id}",
            createdItem.Title ?? "Untitled",
            createdItem.Id
        );

        return MapToDto(createdItem);
    }

    public async Task<CinemaMediaItemDto?> UpdateMediaItemAsync(
        Guid id,
        UpdateMediaItemRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        var existingItem = await repository.GetByIdAsync(id, cancellationToken);
        if (existingItem == null)
        {
            logger.LogWarning("Media item not found for update: {Id}", id);
            return null;
        }

        if (request.Title != null)
        {
            existingItem.Title = request.Title;
        }

        if (request.Description != null)
        {
            existingItem.Description = request.Description;
        }

        if (request.MediaUrl != null)
        {
            existingItem.MediaUrl = request.MediaUrl;
        }

        if (request.Status.HasValue)
        {
            existingItem.Status = request.Status.Value;
        }

        if (request.Priority.HasValue)
        {
            existingItem.Priority = request.Priority.Value;
        }

        if (request.ScheduledFor.HasValue)
        {
            existingItem.ScheduledFor = request.ScheduledFor.Value;
        }

        if (request.Notes != null)
        {
            existingItem.Notes = request.Notes;
        }

        if (request.IsNext.HasValue)
        {
            existingItem.IsNext = request.IsNext.Value;
        }

        existingItem.LastModified = DateTime.Now;

        var updatedItem = await repository.UpdateAsync(existingItem, cancellationToken);
        logger.LogInformation("Updated media item: {Id}", id);

        return updatedItem != null ? MapToDto(updatedItem) : null;
    }

    public async Task<bool> DeleteMediaItemAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        if (id == Guid.Empty)
        {
            return false;
        }

        var result = await repository.DeleteAsync(id, cancellationToken);
        if (result)
        {
            logger.LogInformation("Deleted media item: {Id}", id);
        }
        else
        {
            logger.LogWarning("Media item not found for deletion: {Id}", id);
        }

        return result;
    }

    public async Task<bool> MarkAsNextAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return false;
        }

        var existingItem = await repository.GetByIdAsync(id, cancellationToken);
        if (existingItem == null)
        {
            logger.LogWarning("Media item not found for marking as next: {Id}", id);
            return false;
        }

        await repository.ResetNextFlagsAsync(cancellationToken);
        existingItem.IsNext = true;
        existingItem.LastModified = DateTime.Now;

        var updatedItem = await repository.UpdateAsync(existingItem, cancellationToken);
        logger.LogInformation("Marked media item as next: {Id} - {Title}", id, existingItem.Title);

        return updatedItem != null;
    }

    public async Task<bool> ChangeStatusAsync(
        Guid id,
        MediaStatus status,
        CancellationToken cancellationToken = default
    )
    {
        if (id == Guid.Empty)
        {
            return false;
        }

        var existingItem = await repository.GetByIdAsync(id, cancellationToken);
        if (existingItem == null)
        {
            logger.LogWarning("Media item not found for status change: {Id}", id);
            return false;
        }

        existingItem.Status = status;
        existingItem.LastModified = DateTime.Now;

        var updatedItem = await repository.UpdateAsync(existingItem, cancellationToken);
        logger.LogInformation("Changed status of media item {Id} to {Status}", id, status);

        return updatedItem != null;
    }

    public async Task<bool> ChangePriorityAsync(
        Guid id,
        int priority,
        CancellationToken cancellationToken = default
    )
    {
        if (id == Guid.Empty)
        {
            return false;
        }

        var existingItem = await repository.GetByIdAsync(id, cancellationToken);
        if (existingItem == null)
        {
            logger.LogWarning("Media item not found for priority change: {Id}", id);
            return false;
        }

        existingItem.Priority = priority;
        existingItem.LastModified = DateTime.Now;

        var updatedItem = await repository.UpdateAsync(existingItem, cancellationToken);
        logger.LogInformation("Changed priority of media item {Id} to {Priority}", id, priority);

        return updatedItem != null;
    }

    public async Task<CinemaQueueStatistics> GetStatisticsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var stats = new CinemaQueueStatistics
        {
            PendingItems = await repository.GetCountByStatusAsync(
                MediaStatus.Pending,
                cancellationToken
            ),
            InProgressItems = await repository.GetCountByStatusAsync(
                MediaStatus.InProgress,
                cancellationToken
            ),
            CompletedItems = await repository.GetCountByStatusAsync(
                MediaStatus.Completed,
                cancellationToken
            ),
            CancelledItems = await repository.GetCountByStatusAsync(
                MediaStatus.Cancelled,
                cancellationToken
            ),
            PostponedItems = await repository.GetCountByStatusAsync(
                MediaStatus.Postponed,
                cancellationToken
            ),
        };

        stats.TotalItems =
            stats.PendingItems
            + stats.InProgressItems
            + stats.CompletedItems
            + stats.CancelledItems
            + stats.PostponedItems;

        return stats;
    }

    private static CinemaMediaItemDto MapToDto(CinemaMediaItem item)
    {
        return new CinemaMediaItemDto
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            MediaUrl = item.MediaUrl,
            Status = item.Status,
            Priority = item.Priority,
            CreatedAt = item.CreatedAt,
            ScheduledFor = item.ScheduledFor,
            TwitchUserId = item.TwitchUserId,
            Notes = item.Notes,
            IsNext = item.IsNext,
            LastModified = item.LastModified,
        };
    }
}
