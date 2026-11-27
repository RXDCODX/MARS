using MARS.MediaStorage.Domain.Entities;
using MARS.Shared.Contracts.MediaStorage;

namespace MARS.MediaStorage.Application.Downloads;

public interface IMediaDownloadJobRepository
{
    Task<MediaDownloadJob> AddAsync(MediaDownloadJob job, CancellationToken cancellationToken = default);

    Task<MediaDownloadJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task<MediaDownloadJob?> FindReusableAsync(string cacheKey, DateTimeOffset nowUtc, CancellationToken cancellationToken = default);

    Task<int> CountRecentByActorAsync(string actorId, DateTimeOffset sinceUtc, CancellationToken cancellationToken = default);

    Task<int> CountActiveByActorAsync(string actorId, CancellationToken cancellationToken = default);

    Task<MediaDownloadJob?> ClaimPendingAsync(DateTimeOffset nowUtc, TimeSpan leaseDuration, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaDownloadJob>> GetExpiredAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default);

    Task UpdateAsync(MediaDownloadJob job, CancellationToken cancellationToken = default);

    Task MarkDeliveryCompletedAsync(Guid jobId, Guid executionId, CancellationToken cancellationToken = default);
}

public interface IMediaDownloadJobService
{
    Task<MediaDownloadJob> CreateAsync(
        CreateDownloadJobRequest request,
        CancellationToken cancellationToken = default
    );

    Task<MediaDownloadJob?> GetAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task<MediaDownloadJob?> GetByShortIdAsync(string shortJobId, CancellationToken cancellationToken = default);


    Task<MediaDownloadJob?> CancelAsync(Guid jobId, CancellationToken cancellationToken = default);
}

public interface IMediaSourceDownloader
{
    Task<MediaSourceDownloadResult> DownloadAsync(MediaDownloadJob job, string workingDirectory, CancellationToken cancellationToken = default);
}

public sealed record MediaSourceDownloadResult(
    string SourcePath,
    string? AudioPath,
    string Title,
    TimeSpan Duration,
    MediaDownloadArtifactKind Kind,
    string MimeType
);
