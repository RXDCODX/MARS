using MARS.TTS.Hubs.Models;
using MARS.TTS.Models;

namespace MARS.TTS.Services;

public interface ITtsHubBroadcaster
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
