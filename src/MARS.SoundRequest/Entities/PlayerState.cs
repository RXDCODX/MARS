using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MARS.SoundRequest.Entities;

public class PlayerState
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [NotMapped]
    public Guid StateVersion { get; set; } = Guid.CreateVersion7(DateTime.Now);

    public Guid? CurrentQueueItemId { get; set; }

    public TimeSpan? CurrentTrackProgress { get; set; }

    public PlaybackState State { get; set; } = PlaybackState.Stopped;

    public VideoDisplay VideoState { get; set; } = VideoDisplay.Video;

    public bool IsMuted { get; set; }

    public bool PausedByMute { get; set; }

    public float Volume { get; set; } = 100f;

    [ForeignKey(nameof(CurrentQueueItemId))]
    public QueueItem? CurrentQueueItem { get; set; }
}
