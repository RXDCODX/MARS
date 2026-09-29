using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MARS.CinemaQueue.Entities;

[Table("CinemaQueue")]
public class CinemaMediaItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public string? Title { get; set; }
    public string? Description { get; set; }

    [Required]
    [MaxLength(1000)]
    public required string MediaUrl { get; set; }

    public MediaStatus Status { get; set; } = MediaStatus.Pending;
    public int Priority { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ScheduledFor { get; set; }

    [MaxLength(50)]
    public string? TwitchUserId { get; set; }

    public string? Notes { get; set; }
    public bool IsNext { get; set; } = false;
    public DateTime? LastModified { get; set; } = DateTime.Now;
}
