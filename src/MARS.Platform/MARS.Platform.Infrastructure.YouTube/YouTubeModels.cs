using System.Diagnostics.CodeAnalysis;

namespace MARS.Platform.Infrastructure.YouTube;

/// <summary>
/// Видео, найденное на YouTube.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record YouTubeVideoInfo(
    string Id,
    string Url,
    string Title,
    string? Author,
    TimeSpan? Duration,
    string? ThumbnailUrl
);
