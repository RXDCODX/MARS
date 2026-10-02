namespace MARS.TwitchCore.Services.YouTube;

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
/// Плейлист, найденный по запросу.
/// </summary>
public sealed record YouTubePlaylistInfo(string Id, string Title, string? Author);
