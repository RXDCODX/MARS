namespace MARS.Shared.Contracts.Voice;

public class PlayTtsEvent : OverlayEventAbstract {
    public TtsUser? User { get; set; }
    public string Message { get; set; } = "";
}
