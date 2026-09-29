using System.ComponentModel.DataAnnotations;

namespace MARS.TTS.Models;

public class TwitchUser
{
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

    [MaxLength(20)]
    public string? ChatColor { get; set; }

    public bool IsModerator { get; set; }

    public bool IsVip { get; set; }

    public DateTime? FollowedAt { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.Now;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [MaxLength(100)]
    public string? AliasNickname { get; set; }

    public bool IsInBlockList { get; set; }
}
