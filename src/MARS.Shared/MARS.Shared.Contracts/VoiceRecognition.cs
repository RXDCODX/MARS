namespace MARS.Shared.Contracts.Voice;

public class TtsUser
{
    public string TwitchId { get; set; } = "";
    public string UserLogin { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string ProfileImageUrl { get; set; } = "";
    public string ChatColor { get; set; } = "";
    public bool IsModerator { get; set; }
    public bool IsVip { get; set; }
    public string FollowedAt { get; set; } = "";
    public bool HasFollowedAt { get; set; }
    public string LastUpdated { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public string AliasNickname { get; set; } = "";
    public bool IsInBlockList { get; set; }
}

public class PlayTtsEvent
{
    public TtsUser? User { get; set; }
    public string Message { get; set; } = "";
}

public class TtsStateUpdate
{
    public bool IsStopped { get; set; }
    public double Volume { get; set; }
}

public class ReassignVoiceEvent
{
    public string UserId { get; set; } = "";
}

public class VoiceEvent
{
    public PlayTtsEvent? PlayTts { get; set; }
    public TtsStateUpdate? UpdateTtsState { get; set; }
    public ReassignVoiceEvent? ReassignVoice { get; set; }

    public enum EventOneofCase
    {
        None = 0,
        PlayTts = 1,
        UpdateTtsState = 2,
        ReassignVoice = 3,
    }

    public EventOneofCase EventCase
    {
        get
        {
            if (PlayTts is not null)
                return EventOneofCase.PlayTts;
            if (UpdateTtsState is not null)
                return EventOneofCase.UpdateTtsState;
            if (ReassignVoice is not null)
                return EventOneofCase.ReassignVoice;
            return EventOneofCase.None;
        }
    }
}
