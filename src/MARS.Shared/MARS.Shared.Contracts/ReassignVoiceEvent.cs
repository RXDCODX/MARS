namespace MARS.Shared.Contracts.Voice;

public class ReassignVoiceEvent : OverlayEventAbstract {
    public string UserId { get; set; } = "";
}
