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
