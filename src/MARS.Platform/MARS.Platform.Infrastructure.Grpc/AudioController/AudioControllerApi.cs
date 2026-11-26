using Google.Protobuf.WellKnownTypes;
using Grpc.Net.ClientFactory;
using MARS.Shared.Contracts.AudioController;
using AudioModel = MARS.Shared.Contracts.Models.Audio;

namespace MARS.Platform.Infrastructure.Grpc.AudioController;

internal sealed class AudioControllerApi(GrpcClientFactory clientFactory) : IAudioControllerApi
{
    public async Task<AudioModel.MuteMixerResponse> MuteAsync(IReadOnlyList<string>? excludedProcessNames, CancellationToken cancellationToken = default)
    {
        var request = new MuteMixerRequest();
        if (excludedProcessNames is not null)
        {
            request.ExcludedProcessNames.AddRange(excludedProcessNames);
        }

        var response = await clientFactory
            .CreateClient<AudioControllerService.AudioControllerServiceClient>("AudioController")
            .MuteMixerAsync(request, cancellationToken: cancellationToken);

        return new AudioModel.MuteMixerResponse
        {
            MutedSessionCount = response.MutedSessionCount,
            MutedProcessNames = response.MutedProcessNames.ToList(),
        };
    }

    public async Task<int> UnmuteAsync(CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<AudioControllerService.AudioControllerServiceClient>("AudioController")
            .UnmuteMixerAsync(new Empty(), cancellationToken: cancellationToken);

        return response.UnmutedSessionCount;
    }

    public async Task<AudioModel.MutedSessionsResponse> GetMutedSessionsAsync(CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<AudioControllerService.AudioControllerServiceClient>("AudioController")
            .GetMutedSessionsAsync(new Empty(), cancellationToken: cancellationToken);

        return new AudioModel.MutedSessionsResponse
        {
            Sessions = response.Sessions.Select(session => new AudioModel.MutedSessionDto
            {
                ProcessId = session.ProcessId,
                ProcessName = session.ProcessName,
                DisplayName = session.DisplayName,
            }).ToList(),
        };
    }

    public async Task<AudioModel.MicrophoneVolumeResponse> GetVolumeAsync(CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<AudioControllerService.AudioControllerServiceClient>("AudioController")
            .GetMicrophoneVolumeAsync(new Empty(), cancellationToken: cancellationToken);

        return new AudioModel.MicrophoneVolumeResponse
        {
            Volume = response.Volume,
            VolumePercent = response.VolumePercent,
            IsMuted = response.IsMuted,
            IsAtMaximum = response.IsAtMaximum,
            DeviceName = response.DeviceName,
        };
    }

    public async Task<AudioModel.MicrophoneVolumeSetResponse> SetVolumeAsync(float volume, CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<AudioControllerService.AudioControllerServiceClient>("AudioController")
            .SetMicrophoneVolumeAsync(new SetMicrophoneVolumeRequest { Volume = volume }, cancellationToken: cancellationToken);

        return new AudioModel.MicrophoneVolumeSetResponse
        {
            PreviousVolume = response.PreviousVolume,
            PreviousVolumePercent = response.PreviousVolumePercent,
            CurrentVolume = response.CurrentVolume,
            CurrentVolumePercent = response.CurrentVolumePercent,
            Message = response.Message,
        };
    }

    public async Task<AudioModel.MicrophoneVolumeSetResponse> SetVolumeToMaxAsync(CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<AudioControllerService.AudioControllerServiceClient>("AudioController")
            .SetMicrophoneVolumeToMaxAsync(new Empty(), cancellationToken: cancellationToken);

        return new AudioModel.MicrophoneVolumeSetResponse
        {
            PreviousVolume = response.PreviousVolume,
            PreviousVolumePercent = response.PreviousVolumePercent,
            CurrentVolume = response.CurrentVolume,
            CurrentVolumePercent = response.CurrentVolumePercent,
            Message = response.Message,
        };
    }
}