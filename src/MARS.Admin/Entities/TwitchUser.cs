using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MARS.Admin.Entities;

/// <summary>
/// Минимальная заглушка TwitchUser для MARS.Admin
/// </summary>
[Table("TwitchUsers")]
public class TwitchUser
{
    [Key]
    [Required]
    [MaxLength(50)]
    public required string TwitchId { get; init; }

    [Required]
    [MaxLength(100)]
    public required string UserLogin { get; set; }

    [Required]
    [MaxLength(100)]
    public required string DisplayName { get; set; }

    [MaxLength(500)]
    public string? ProfileImageUrl { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.Now;
}
