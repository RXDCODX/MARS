namespace MARS.Discord.Services.YouTube;

/// <summary>
/// Видео, найденное на YouTube.
///
/// Отдельный тип вместо класса из YoutubeExplode: библиотека вешает процесс при
/// загрузке сборки, и проверять сервис, который к ней обращается, было бы
/// невозможно. Наружу отдаются только те поля, которыми реально пользуется
/// резолвер.
/// </summary>
public sealed record YouTubeVideoInfo(
    string Id,
    string Url,
    string Title,
    string? Author,
    TimeSpan? Duration,
    string? ThumbnailUrl
);

/// <summary>
/// Аудиопоток видео: контейнер и битрейт нужны, чтобы выбрать лучший поток.
/// </summary>
/// <param name="IsAudioOnly">Аудио без видео предпочтительнее muxed-потока.</param>
public sealed record YouTubeAudioStream(string Container, long BitsPerSecond, bool IsAudioOnly);

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
    Task<IReadOnlyList<YouTubeVideoInfo>> SearchVideosAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken = default
    );

    Task<YouTubeVideoInfo?> GetVideoAsync(
        string videoUrlOrId,
        CancellationToken cancellationToken = default
    );

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
