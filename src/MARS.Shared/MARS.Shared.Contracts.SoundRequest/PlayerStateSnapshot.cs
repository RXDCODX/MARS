namespace MARS.Shared.Contracts.SoundRequest;

public class PlayerStateSnapshot
{
    public string Id { get; set; } = "";
    public string CurrentQueueItemId { get; set; } = "";
    public long CurrentTrackProgressSeconds { get; set; }
    public bool HasCurrentTrackProgress { get; set; }
    public SoundRequestPlaybackState State { get; set; }
    public SoundRequestVideoDisplay VideoState { get; set; }
    public bool IsMuted { get; set; }
    public bool PausedByMute { get; set; }
    public float Volume { get; set; }
    public QueueItemSnapshot? CurrentQueueItem { get; set; }
}
