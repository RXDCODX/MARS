namespace MARS.Platform.Infrastructure.YouTube;

/// <summary>
/// Видео, найденное на YouTube.
/// </summary>
public sealed record YouTubeVideoInfo(
    string Id,
    string Url,
    string Title,
    string? Author,
    TimeSpan? Duration,
    string? ThumbnailUrl
);
