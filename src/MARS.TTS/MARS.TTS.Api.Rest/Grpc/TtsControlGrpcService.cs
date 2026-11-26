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

        logger.LogInformation("SetMuteState: IsMuted={IsMuted}, Volume={Volume}", request.IsMuted, request.Volume);

        await notifier.BroadcastStateAsync(
            new TtsState { IsStopped = request.IsMuted, Volume = request.Volume },
            context.CancellationToken
        );

        return new Empty();
    }
}