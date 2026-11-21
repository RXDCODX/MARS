namespace MARS.WaifuGacha.Models.Rest;

/// <summary>
/// Базовая сущность вайфу (владелец — MARS.WaifuGacha) в виде DTO для
/// межсервисного обмена. Зеркало <c>MARS.WaifuGacha.Domain.Entities.Waifu</c>
/// без ORM-артефактов и DB-специфики.
/// </summary>
public sealed record WaifuDto(
    string ShikiId,
    string Name,
    long Age,
    string? Anime,
    string? Manga,
    string ImageUrl,
    bool IsPrivated,
    int OrderCount
);

/// <summary>
/// Базовая сущность супруга (хоста) для межсервисного обмена.
/// </summary>
public sealed record HusbandDto(
    string TwitchId,
    string? WaifuBrideId,
    bool IsPrivated,
    long OrderCount,
    DateTime WhenOrdered
);

/// <summary>
/// Базовая сущность фумо для межсервисного обмена.
/// </summary>
public sealed record FumoDto(
    int MfcId,
    string Name,
    string Character,
    string? Origin,
    string ThumbnailUrl,
    int OrderCount
);

/// <summary>
/// Базовая сущность модуля Мику для межсервисного обмена.
/// </summary>
public sealed record MikuModuleDto(
    int PageId,
    string Title,
    string? JapaneseName,
    string? Designer,
    string ThumbnailUrl,
    int OrderCount
);

/// <summary>
/// Базовая сущность жабы для межсервисного обмена.
/// </summary>
public sealed record FrogDto(
    int Pid,
    string CommonName,
    string ScientificName,
    string? RussianName,
    string ThumbnailUrl,
    int OrderCount
);