using MARS.TTS.Application.Models;

namespace MARS.TTS.Application.Ports;

public interface ITtsNotifier
{
    double CurrentVolume { get; }

    Task BroadcastAsync(
        TwitchUser? user,
        string message,
        CancellationToken cancellationToken = default
    );

    Task BroadcastStateAsync(TtsState? state, CancellationToken cancellationToken = default);

    Task BroadcastReassignVoiceAsync(string userId, CancellationToken cancellationToken = default);
}
