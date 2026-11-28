using MARS.MediaStorage.Domain.Entities;
using System.Diagnostics.CodeAnalysis;

namespace MARS.MediaStorage.Application.Downloads;

public interface IMediaSourceDownloader
{
    Task<MediaSourceDownloadResult> DownloadAsync(
        MediaDownloadJob job,
        string workingDirectory,
        CancellationToken cancellationToken = default
    );
}