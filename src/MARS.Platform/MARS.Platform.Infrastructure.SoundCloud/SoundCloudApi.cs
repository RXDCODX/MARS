using System.Diagnostics.CodeAnalysis;
﻿using SoundCloudExplode;
using SoundCloudExplode.Tracks;

namespace MARS.Platform.Infrastructure.SoundCloud;

[ExcludeFromCodeCoverage]
public sealed class SoundCloudApi : ISoundCloudApi
{
    private readonly SoundCloudClient _client = new();

    public async Task<SoundCloudTrackInfo?> GetTrackAsync(
        string url,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        try
        {
            var track = await _client.Tracks.GetAsync(url, cancellationToken);
            if (track is null)
            {
                return null;
            }

            var duration = track.Duration.HasValue
                ? TimeSpan.FromMilliseconds(track.Duration.Value)
                : (TimeSpan?)null;

            return new SoundCloudTrackInfo(
                track.Id.ToString(),
                track.PermalinkUrl?.ToString() ?? url,
                string.IsNullOrWhiteSpace(track.Title) ? "Unknown SoundCloud Track" : track.Title,
                track.User?.Username,
                duration,
                track.ArtworkUrl?.ToString()
            );
        }
        catch
        {
            return null;
        }
    }

    public async Task DownloadAsync(
        string url,
        string filePath,
        CancellationToken cancellationToken = default
    )
    {
        var track = await _client.Tracks.GetAsync(url, cancellationToken);
        if (track is null)
        {
            throw new InvalidOperationException("SoundCloud track not found.");
        }

        await _client.DownloadAsync(
            track,
            filePath,
            progress: null,
            headers: null,
            cancellationToken
        );
    }
}
