using MARS.CinemaQueue.Entities;

namespace MARS.CinemaQueue.Interfaces;

public interface ICinemaQueueService
{
    Task<IEnumerable<CinemaMediaItemDto>> GetAllMediaItemsAsync(
        CancellationToken cancellationToken = default
    );
    Task<CinemaMediaItemDto?> GetMediaItemByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );
    Task<CinemaMediaItemDto?> GetNextMediaItemAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CinemaMediaItemDto>> GetMediaItemsByStatusAsync(
        MediaStatus status,
        CancellationToken cancellationToken = default
    );
    Task<CinemaMediaItemDto> CreateMediaItemAsync(
        CreateMediaItemRequest request,
        CancellationToken cancellationToken = default
    );
    Task<CinemaMediaItemDto?> UpdateMediaItemAsync(
        Guid id,
        UpdateMediaItemRequest request,
        CancellationToken cancellationToken = default
    );
    Task<bool> DeleteMediaItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> MarkAsNextAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ChangeStatusAsync(
        Guid id,
        MediaStatus status,
        CancellationToken cancellationToken = default
    );
    Task<bool> ChangePriorityAsync(
        Guid id,
        int priority,
        CancellationToken cancellationToken = default
    );
    Task<CinemaQueueStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}
