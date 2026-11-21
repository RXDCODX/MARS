namespace MARS.WaifuGacha.Models.Rest;

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
