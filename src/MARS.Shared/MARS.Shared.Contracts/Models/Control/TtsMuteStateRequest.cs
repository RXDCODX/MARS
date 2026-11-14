namespace MARS.Shared.Contracts.Models.Control;

/// <summary>
/// Состояние глушения TTS.
/// </summary>
/// <remarks>
/// Контракт внутреннего REST-вызова <c>POST api/internal/tts/mute-state</c>.
/// Раньше это был unary-метод <c>VoiceRecognitionService.SetMuteState</c>, и
/// координатор глушения звука в MARS.Alerts звал его, чтобы синтез замолчал
/// вместе с микшером и плеером.
/// </remarks>
public class TtsMuteStateRequest
{
    /// <summary>Заглушён ли синтез.</summary>
    public bool IsMuted { get; set; }

    /// <summary>Громкость, которую нужно выставить вместе с состоянием.</summary>
    public double Volume { get; set; }
}
