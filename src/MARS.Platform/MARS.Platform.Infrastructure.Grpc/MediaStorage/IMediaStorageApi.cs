using MediaModel = MARS.Shared.Contracts.Models.Media;
using ProtoMedia = MARS.Shared.Contracts.MediaStorage;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Platform.Infrastructure.Grpc.MediaStorage;

public interface IMediaStorageApi
{
    Task<MediaModel.MediaInfo?> GetMediaInfoAsync(
        Guid mediaInfoId,
        CancellationToken cancellationToken = default
    );

    Task<MediaModel.MediaInfo?> GetNextRandomMemeAsync(
        string folderPath,
        string? displayName,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<MediaModel.MediaInfo>?> GetAllAlertsAsync(
        CancellationToken cancellationToken = default
    );

    Task<string?> UploadMemoryFileAsync(
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default
    );

    Task<ProtoMedia.DownloadJobResponse> CreateDownloadJobAsync(
        ProtoMedia.CreateDownloadJobRequest request,
        CancellationToken cancellationToken = default
    );

    Task<ProtoMedia.DownloadJobResponse> GetDownloadJobAsync(
        ProtoMedia.GetDownloadJobRequest request,
        CancellationToken cancellationToken = default
    );

    Task<ProtoMedia.DownloadJobResponse> CancelDownloadJobAsync(
        ProtoMedia.GetDownloadJobRequest request,
        CancellationToken cancellationToken = default
    );
}
