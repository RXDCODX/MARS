using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MARS.SoundRequest.Entities;

public class QueueItem
{
    [Key]
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required]
    public required Guid TrackId { get; set; }

    [ForeignKey(nameof(TrackId))]
    public BaseTrackInfo? Track { get; set; }

    [Required]
    public required int QueueOrder { get; set; }

    [MaxLength(50)]
    [Required]
    public required string RequestedByTwitchId { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.Now;
}
