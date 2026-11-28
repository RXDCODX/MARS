using MARS.MediaStorage.Domain.Entities;
using MARS.Shared.Contracts.MediaStorage;
using System.Diagnostics.CodeAnalysis;

namespace MARS.MediaStorage.Application.Downloads;

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