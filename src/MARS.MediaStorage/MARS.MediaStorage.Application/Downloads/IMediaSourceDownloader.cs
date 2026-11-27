using MARS.MediaStorage.Domain.Entities;

namespace MARS.MediaStorage.Application.Downloads;

public interface IMediaSourceDownloader
{
    Task<MediaSourceDownloadResult> DownloadAsync(
        MediaDownloadJob job,
        string workingDirectory,
        CancellationToken cancellationToken = default
    );
}