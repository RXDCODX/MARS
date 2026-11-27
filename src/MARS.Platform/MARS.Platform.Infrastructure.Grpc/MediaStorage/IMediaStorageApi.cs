using MediaModel = MARS.Shared.Contracts.Models.Media;
using ProtoMedia = MARS.Shared.Contracts.MediaStorage;

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
}
