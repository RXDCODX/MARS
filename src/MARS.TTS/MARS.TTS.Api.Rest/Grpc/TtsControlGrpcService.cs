using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MARS.Shared.Contracts.Tts.Control;
using MARS.TTS.Application.Models;
using MARS.TTS.Application.Ports;
using MARS.TTS.Application.Services;
using Microsoft.Extensions.Logging;

namespace MARS.TTS.Api.Rest.Grpc;

public sealed class TtsControlGrpcService(
    ITtsNotifier notifier,
    ITtsMessageFilterService filterService,
    ILogger<TtsControlGrpcService> logger
) : TtsControlService.TtsControlServiceBase
{
    public override async Task<Empty> SetMuteState(
        TtsMuteStateRequest request,
        ServerCallContext context
    )
    {
        if (request is null)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Состояние не передано"));
        }

        logger.LogInformation(
            "SetMuteState: IsMuted={IsMuted}, Volume={Volume}",
            request.IsMuted,
            request.Volume
        );

        await notifier.BroadcastStateAsync(
            new TtsState { IsStopped = request.IsMuted, Volume = request.Volume },
            context.CancellationToken
        );

        return new Empty();
    }

    public override Task<Empty> SetFilterEnabled(TtsFilterRequest request, ServerCallContext context)
    {
        filterService.IsFilterEnabled = request?.Enabled ?? false;
        logger.LogInformation("TTS filter enabled: {Enabled}", filterService.IsFilterEnabled);
        return Task.FromResult(new Empty());
    }

    public override Task<BoolResult> ToggleFilter(Empty request, ServerCallContext context)
    {
        filterService.IsFilterEnabled = !filterService.IsFilterEnabled;
        logger.LogInformation("TTS filter toggled: {Enabled}", filterService.IsFilterEnabled);
        return Task.FromResult(new BoolResult { Success = filterService.IsFilterEnabled });
    }

    public override async Task<Empty> ReassignVoice(TtsReassignVoiceRequest request, ServerCallContext context)
    {
        if (!string.IsNullOrWhiteSpace(request?.UserId))
        {
            await notifier.BroadcastReassignVoiceAsync(request.UserId, context.CancellationToken);
        }

        return new Empty();
    }

    public override Task<TtsVolumeResponse> GetVolume(Empty request, ServerCallContext context)
    {
        return Task.FromResult(new TtsVolumeResponse { Volume = notifier.CurrentVolume });
    }

    public override async Task<Empty> SetVolume(TtsVolumeRequest request, ServerCallContext context)
    {
        var volume = Math.Clamp(request?.Volume ?? notifier.CurrentVolume, 0.0, 2.0);
        await notifier.BroadcastStateAsync(new TtsState { IsStopped = false, Volume = volume }, context.CancellationToken);
        return new Empty();
    }
}
