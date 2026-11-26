using MARS.Shared.Contracts.AudioController;
using AudioModel = MARS.Shared.Contracts.Models.Audio;

namespace MARS.Platform.Infrastructure.Grpc.AudioController;

public interface IAudioControllerApi
{
    Task<AudioModel.MuteMixerResponse> MuteAsync(
        IReadOnlyList<string>? excludedProcessNames,
        CancellationToken cancellationToken = default
    );

    Task<int> UnmuteAsync(CancellationToken cancellationToken = default);

    Task<AudioModel.MutedSessionsResponse> GetMutedSessionsAsync(
        CancellationToken cancellationToken = default
    );

    Task<AudioModel.MicrophoneVolumeResponse> GetVolumeAsync(
        CancellationToken cancellationToken = default
    );

    Task<AudioModel.MicrophoneVolumeSetResponse> SetVolumeAsync(
        float volume,
        CancellationToken cancellationToken = default
    );

    Task<AudioModel.MicrophoneVolumeSetResponse> SetVolumeToMaxAsync(
        CancellationToken cancellationToken = default
    );
}
