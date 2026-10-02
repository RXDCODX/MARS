using System.ComponentModel.DataAnnotations;

namespace MARS.Shikimori.Entities;

/// <summary>
/// Персонаж Shikimori в базе сервиса.
/// </summary>
/// <remarks>
/// Доменная замена монолитных GraphQL-узлов (пункт W3): наружу отдаётся не
/// ответ внешнего API, а собственная модель. Названия аниме и манги хранятся
/// уже выбранными — самыми короткими среди произведений персонажа, как
/// выбирал монолитный <c>GetCharacterAnimeTitle</c>.
/// </remarks>
public class ShikimoriCharacter
{
    [Key]
    public long Id { get; set; }

    [MaxLength(512)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? RussianName { get; set; }

    public string? Description { get; set; }

    /// <summary>Абсолютный URL картинки: наружу относительные пути не отдаются.</summary>
    [MaxLength(1024)]
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Путь картинки так, как его отдал Shikimori. Именно такая форма хранится в
    /// базе MARS.WaifuGacha, поэтому наружу уходят оба варианта.
    /// </summary>
    [MaxLength(1024)]
    public string ImagePath { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? AnimeTitle { get; set; }

    [MaxLength(512)]
    public string? MangaTitle { get; set; }

    /// <summary>Когда данные последний раз брались у Shikimori.</summary>
    public DateTime SyncedAtUtc { get; set; } = DateTime.UtcNow;
}
