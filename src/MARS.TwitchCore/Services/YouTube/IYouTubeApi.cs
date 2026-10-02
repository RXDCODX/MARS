namespace MARS.TwitchCore.Services.YouTube;

/// <summary>
/// Поверхность YouTube, которой пользуется резолвер.
///
/// Интерфейс существует из-за YoutubeExplode: <c>YoutubeClient</c> умеет только
/// скачивать и искать по сети, в тесте он не собирается, а обращение к его типам
/// вешает процесс при загрузке сборки. Поэтому всё, что нужно резолверу, описано
/// своими типами, а работа с библиотекой осталась в реализации по интерфейсу.
/// </summary>
public interface IYouTubeApi
{
    Task<IReadOnlyList<YouTubePlaylistInfo>> SearchPlaylistsAsync(
        string query,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<YouTubeVideoInfo>> SearchVideosAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<YouTubeVideoInfo>> GetPlaylistVideosAsync(
        string playlistUrl,
        CancellationToken cancellationToken = default
    );

    Task<YouTubeVideoInfo?> GetVideoAsync(
        string videoUrlOrId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Аудиопотоки видео: отбор лучшего остаётся у резолвера, библиотека только
    /// отдаёт список.
    /// </summary>
    Task<IReadOnlyList<YouTubeAudioStream>> GetAudioStreamsAsync(
        string videoId,
        CancellationToken cancellationToken = default
    );

    Task DownloadAsync(
        string videoId,
        YouTubeAudioStream stream,
        string filePath,
        CancellationToken cancellationToken = default
    );
}
