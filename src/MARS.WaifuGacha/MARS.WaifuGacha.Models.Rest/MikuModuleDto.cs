namespace MARS.WaifuGacha.Models.Rest;

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
