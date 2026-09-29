using MARS.CinemaQueue.Entities;

namespace MARS.CinemaQueue.Interfaces;

public interface ICinemaQueueRepository
{
    Task<IEnumerable<CinemaMediaItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CinemaMediaItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CinemaMediaItem?> GetNextAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CinemaMediaItem>> GetByStatusAsync(
        MediaStatus status,
        CancellationToken cancellationToken = default
    );
    Task<CinemaMediaItem> CreateAsync(
        CinemaMediaItem cinemaMediaItem,
        CancellationToken cancellationToken = default
    );
    Task<CinemaMediaItem?> UpdateAsync(
        CinemaMediaItem cinemaMediaItem,
        CancellationToken cancellationToken = default
    );
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task ResetNextFlagsAsync(CancellationToken cancellationToken = default);
    Task<int> GetCountByStatusAsync(
        MediaStatus status,
        CancellationToken cancellationToken = default
    );
}
