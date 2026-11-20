namespace MARS.Shared.Contracts.SoundRequest;

public enum SoundRequestPlaybackState
{
    Stopped = 0,
    Playing = 1,
    Paused = 2,
    SwitchingTrack = 3,
    WaitingForTrack = 4,
}

public enum SoundRequestVideoDisplay
{
    Video = 0,
    NoVideo = 1,
    AudioOnly = 2,
}

public class TrackInfo
{
    public string Id { get; set; } = "";
    public string TrackName { get; set; } = "";
    public List<string> Authors { get; set; } = new();
    public long DurationSeconds { get; set; }
    public string Url { get; set; } = "";
    public string LastTimePlays { get; set; } = "";
    public string ArtworkUrl { get; set; } = "";
    public string VideoId { get; set; } = "";
    public bool IsDeleted { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}

public class QueueItemSnapshot
{
    public string Id { get; set; } = "";
    public string TrackId { get; set; } = "";
    public TrackInfo? Track { get; set; }
    public int QueueOrder { get; set; }
    public string RequestedByTwitchId { get; set; } = "";
    public string RequestedAt { get; set; } = "";
}

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

public class QueueState
{
    public List<QueueItemSnapshot> Queue { get; set; } = new();
}

public class SoundRequestEvent
{
    public PlayerStateSnapshot? PlayerStateChange { get; set; }
    public QueueState? QueueChanged { get; set; }

    public enum EventOneofCase
    {
        None = 0,
        PlayerStateChange = 1,
        QueueChanged = 2,
    }

    public EventOneofCase EventCase
    {
        get
        {
            if (PlayerStateChange is not null)
                return EventOneofCase.PlayerStateChange;
            if (QueueChanged is not null)
                return EventOneofCase.QueueChanged;
            return EventOneofCase.None;
        }
    }
}
