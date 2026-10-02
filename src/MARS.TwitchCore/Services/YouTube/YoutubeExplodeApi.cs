using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;
using PlaylistSearchResult = YoutubeExplode.Search.PlaylistSearchResult;
using PlaylistVideo = YoutubeExplode.Playlists.PlaylistVideo;
using SearchVideoResult = YoutubeExplode.Search.VideoSearchResult;
using YouTubeVideo = YoutubeExplode.Videos.Video;

namespace MARS.TwitchCore.Services.YouTube;

/// <summary>
/// Реализация <see cref="IYouTubeApi"/> поверх YoutubeExplode.
///
/// Это единственный файл сервиса, который знает о библиотеке: всё остальное
/// работает с <see cref="IYouTubeApi"/> и своими типами, поэтому и собирается в
/// тестах, и не зависит от того, как YoutubeExplode устроен изнутри.
/// </summary>
public sealed class YoutubeExplodeApi : IYouTubeApi
{
    private readonly YoutubeClient _client = new();

    public async Task<IReadOnlyList<YouTubePlaylistInfo>> SearchPlaylistsAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        var result = new List<YouTubePlaylistInfo>();

        await foreach (var playlist in _client.Search.GetPlaylistsAsync(query, cancellationToken))
        {
            result.Add(Map(playlist));
        }

        return result;
    }

    public async Task<IReadOnlyList<YouTubeVideoInfo>> SearchVideosAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken = default
    )
    {
        var result = new List<YouTubeVideoInfo>();

        await foreach (var video in _client.Search.GetVideosAsync(query, cancellationToken))
        {
            result.Add(Map(video));

            if (result.Count >= maxResults)
            {
                break;
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<YouTubeVideoInfo>> GetPlaylistVideosAsync(
        string playlistUrl,
        CancellationToken cancellationToken = default
    )
    {
        var result = new List<YouTubeVideoInfo>();

        await foreach (
            var video in _client.Playlists.GetVideosAsync(playlistUrl, cancellationToken)
        )
        {
            result.Add(Map(video));
        }

        return result;
    }

    public async Task<YouTubeVideoInfo?> GetVideoAsync(
        string videoUrlOrId,
        CancellationToken cancellationToken = default
    )
    {
        var video = await _client.Videos.GetAsync(videoUrlOrId, cancellationToken);
        return Map(video);
    }

    public async Task<IReadOnlyList<YouTubeAudioStream>> GetAudioStreamsAsync(
        string videoId,
        CancellationToken cancellationToken = default
    )
    {
        var manifest = await _client.Videos.Streams.GetManifestAsync(videoId, cancellationToken);

        var result = new List<YouTubeAudioStream>();

        foreach (var stream in manifest.GetAudioOnlyStreams())
        {
            result.Add(Map(stream, isAudioOnly: true));
        }

        foreach (var stream in manifest.GetMuxedStreams())
        {
            result.Add(Map(stream, isAudioOnly: false));
        }

        return result;
    }

    public async Task DownloadAsync(
        string videoId,
        YouTubeAudioStream stream,
        string filePath,
        CancellationToken cancellationToken = default
    )
    {
        var manifest = await _client.Videos.Streams.GetManifestAsync(videoId, cancellationToken);

        var streamInfo =
            manifest
                .GetAudioOnlyStreams()
                .Cast<IStreamInfo>()
                .Concat(manifest.GetMuxedStreams())
                .FirstOrDefault(candidate =>
                    candidate.Container.Name == stream.Container
                    && candidate.Bitrate.BitsPerSecond == stream.BitsPerSecond
                )
            ?? throw new InvalidOperationException(
                $"Поток {stream.Container} с битрейтом {stream.BitsPerSecond} больше недоступен"
            );

        await _client.Videos.Streams.DownloadAsync(streamInfo, filePath, null, cancellationToken);
    }

    private static YouTubePlaylistInfo Map(PlaylistSearchResult playlist) =>
        new(playlist.Id, playlist.Title, playlist.Author?.ChannelTitle);

    private static YouTubeVideoInfo Map(SearchVideoResult video) =>
        new(
            video.Id,
            video.Url,
            video.Title,
            video.Author?.ChannelTitle,
            video.Duration,
            LargestThumbnailUrl(video.Thumbnails)
        );

    private static YouTubeVideoInfo Map(PlaylistVideo video) =>
        new(
            video.Id,
            video.Url,
            video.Title,
            video.Author?.ChannelTitle,
            video.Duration,
            LargestThumbnailUrl(video.Thumbnails)
        );

    private static YouTubeVideoInfo Map(YouTubeVideo video) =>
        new(
            video.Id,
            video.Url,
            video.Title,
            video.Author?.ChannelTitle,
            video.Duration,
            LargestThumbnailUrl(video.Thumbnails)
        );

    /// <summary>
    /// Обложка выбирается по площади: маленькие превью рвутся на оверлее.
    /// </summary>
    private static string? LargestThumbnailUrl(IReadOnlyList<Thumbnail> thumbnails) =>
        thumbnails
            .OrderByDescending(thumbnail => thumbnail.Resolution.Area)
            .FirstOrDefault(thumbnail => !string.IsNullOrWhiteSpace(thumbnail.Url))
            ?.Url;

    private static YouTubeAudioStream Map(IStreamInfo stream, bool isAudioOnly) =>
        new(stream.Container.Name, stream.Bitrate.BitsPerSecond, isAudioOnly);
}
