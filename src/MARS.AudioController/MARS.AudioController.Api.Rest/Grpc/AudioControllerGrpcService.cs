using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MARS.AudioController.Application.Configuration;
using MARS.AudioController.Application.Ports;
using MARS.Shared.Contracts.AudioController;
using Microsoft.Extensions.Options;

namespace MARS.AudioController.Api.Rest.Grpc;

public sealed class AudioControllerGrpcService(
    IAudioDeviceService audioDeviceService,
    IAudioMixerService audioMixerService,
    IOptions<AudioControllerConfiguration> configuration
) : AudioControllerService.AudioControllerServiceBase
{
    public override async Task<MuteMixerResponse> MuteMixer(
        MuteMixerRequest request,
        ServerCallContext context
    )
    {
        var excluded =
            request.ExcludedProcessNames.Count == 0
                ? configuration.Value.DefaultExcludedProcessNames
                : request.ExcludedProcessNames.ToList();
        var muted = await audioMixerService.MuteAsync(excluded, context.CancellationToken);
        return new MuteMixerResponse
        {
            MutedSessionCount = muted.Count,
            MutedProcessNames = { muted },
        };
    }

    public override async Task<UnmuteMixerResponse> UnmuteMixer(
        Empty request,
        ServerCallContext context
    )
    {
        var count = await audioMixerService.UnmuteAsync(context.CancellationToken);
        return new UnmuteMixerResponse { UnmutedSessionCount = count };
    }

    public override async Task<MutedSessionsResponse> GetMutedSessions(
        Empty request,
        ServerCallContext context
    )
    {
        var sessions = await audioMixerService.GetMutedSessionsAsync(context.CancellationToken);
        var response = new MutedSessionsResponse();
        response.Sessions.AddRange(
            sessions.Select(s => new MutedSession
            {
                ProcessId = s.ProcessId,
                ProcessName = s.ProcessName,
                DisplayName = s.DisplayName,
            })
        );
        return response;
    }

    public override async Task<MicrophoneVolumeResponse> GetMicrophoneVolume(
        Empty request,
        ServerCallContext context
    )
    {
        var volume = await audioDeviceService.GetVolumeAsync(context.CancellationToken);
        var deviceName = await audioDeviceService.GetDeviceNameAsync(context.CancellationToken);
        var isMuted = await audioDeviceService.IsMutedAsync(context.CancellationToken);

        return new MicrophoneVolumeResponse
        {
            Volume = volume,
            VolumePercent = $"{volume * 100:F0}%",
            IsMuted = isMuted,
            IsAtMaximum = volume >= 1.0f,
            DeviceName = deviceName,
        };
    }

    public override async Task<MicrophoneVolumeSetResponse> SetMicrophoneVolume(
        SetMicrophoneVolumeRequest request,
        ServerCallContext context
    )
    {
        if (request.Volume < 0.0f || request.Volume > 1.0f)
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, "Volume must be between 0.0 and 1.0")
            );
        }

        var previousVolume = await audioDeviceService.GetVolumeAsync(context.CancellationToken);
        await audioDeviceService.SetVolumeAsync(request.Volume, context.CancellationToken);

        return new MicrophoneVolumeSetResponse
        {
            PreviousVolume = previousVolume,
            PreviousVolumePercent = $"{previousVolume * 100:F0}%",
            CurrentVolume = request.Volume,
            CurrentVolumePercent = $"{request.Volume * 100:F0}%",
            Message = $"Microphone volume set to {request.Volume * 100:F0}%",
        };
    }

    public override async Task<MicrophoneVolumeSetResponse> SetMicrophoneVolumeToMax(
        Empty request,
        ServerCallContext context
    )
    {
        var previousVolume = await audioDeviceService.GetVolumeAsync(context.CancellationToken);
        await audioDeviceService.SetVolumeToMaxAsync(context.CancellationToken);

        return new MicrophoneVolumeSetResponse
        {
            PreviousVolume = previousVolume,
            PreviousVolumePercent = $"{previousVolume * 100:F0}%",
            CurrentVolume = 1.0f,
            CurrentVolumePercent = "100%",
            Message = "Microphone volume set to maximum",
        };
    }
}
