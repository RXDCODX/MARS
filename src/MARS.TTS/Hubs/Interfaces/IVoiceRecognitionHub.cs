using MARS.TTS.Hubs.Models;
using MARS.TTS.Models;

namespace MARS.TTS.Hubs.Interfaces;

/// <summary>
/// Interface for TTS hub client methods.
/// Defines messages that can be sent from the server to AudioController consumers.
/// </summary>
public interface IVoiceRecognitionHub
{
    Task PlayTts(TwitchUser user, string message);
    Task UpdateTtsState(TtsState state);
    Task ReassignVoice(string userId);
}
