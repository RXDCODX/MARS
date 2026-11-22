namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Contracts.Voice;

public class VoiceEvent : OverlayEventAbstract
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
