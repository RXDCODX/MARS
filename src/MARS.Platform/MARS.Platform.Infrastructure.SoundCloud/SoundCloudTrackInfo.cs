using System;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Platform.Infrastructure.SoundCloud;

[ExcludeFromCodeCoverage]
public sealed record SoundCloudTrackInfo(
    string Id,
    string Url,
    string Title,
    string? Author,
    TimeSpan? Duration,
    string? ArtworkUrl
);