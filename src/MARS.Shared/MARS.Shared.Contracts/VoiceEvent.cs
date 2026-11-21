namespace MARS.Shared.Contracts.Voice;

public class VoiceEvent
{
    public PlayTtsEvent? PlayTts { get; set; }
    public TtsStateUpdate? UpdateTtsState { get; set; }
    public ReassignVoiceEvent? ReassignVoice { get; set; }

    /// <summary>Имя заполненной ветки без отдельного enum и ручной цепочки проверок.</summary>
    public string? EventCase => OneofEventContract.GetActiveBranch(this)?.Name;
}
