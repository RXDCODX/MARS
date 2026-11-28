using System.Diagnostics.CodeAnalysis;

namespace MARS.Platform.Infrastructure.SoundCloud;

public interface ISoundCloudApi
{
    Task<SoundCloudTrackInfo?> GetTrackAsync(
        string url,
        CancellationToken cancellationToken = default
    );

    Task DownloadAsync(string url, string filePath, CancellationToken cancellationToken = default);
}
