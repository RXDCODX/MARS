using System.ComponentModel.DataAnnotations;

namespace MARS.Shikimori.Entities;

/// <summary>
/// Произведение, выданное командой «случайное аниме/манга». В базе остаётся
/// история выдач: по ней видно, что уже показывали зрителям.
/// </summary>
public class ShikimoriTitlePick
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>anime или manga.</summary>
    [MaxLength(16)]
    public string Kind { get; set; } = string.Empty;

    public long ShikimoriId { get; set; }

    [MaxLength(512)]
    public string? Name { get; set; }

    [MaxLength(512)]
    public string? RussianName { get; set; }

    public int? Year { get; set; }

    [MaxLength(1024)]
    public string Url { get; set; } = string.Empty;

    public DateTime PickedAtUtc { get; set; } = DateTime.UtcNow;
}
