namespace MARS.Twitch.Api.Services.YouTube;

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
