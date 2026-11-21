namespace MARS.WaifuGacha.Models.Rest;

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
