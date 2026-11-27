using System;

namespace MARS.Platform.Infrastructure.SoundCloud;

public sealed record SoundCloudTrackInfo(
    string Id,
    string Url,
    string Title,
    string? Author,
    TimeSpan? Duration,
    string? ArtworkUrl
);