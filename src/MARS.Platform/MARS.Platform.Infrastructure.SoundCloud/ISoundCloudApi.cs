namespace MARS.Platform.Infrastructure.SoundCloud;

public sealed record SoundCloudTrackInfo(
    string Id,
    string Url,
    string Title,
    string? Author,
    TimeSpan? Duration,
    string? ArtworkUrl
);

public interface ISoundCloudApi
{
    Task<SoundCloudTrackInfo?> GetTrackAsync(string url, CancellationToken cancellationToken = default);

    Task DownloadAsync(string url, string filePath, CancellationToken cancellationToken = default);
}
