namespace MARS.Shared.Contracts.Media;

public class MediaMetaPayload
{
    public int TwitchPointsCost { get; set; }
    public string TwitchGuid { get; set; } = "";
    public bool Vip { get; set; }
    public string DisplayName { get; set; } = "";
    public bool IsLooped { get; set; }
    public bool IsFreezeRequired { get; set; }
    public int Duration { get; set; }
    public MediaAlertPriorityKind Priority { get; set; }
    public int Volume { get; set; }
    public bool IsEnabled { get; set; }
}
