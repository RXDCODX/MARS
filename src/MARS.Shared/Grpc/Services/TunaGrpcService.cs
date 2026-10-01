using Grpc.Core;
using MARS.Shared.Grpc.Models;
using MARS.Shared.Grpc.Tuna;

namespace MARS.Shared.Grpc.Services;

public sealed class TunaGrpcService(GrpcEventBroadcaster<TunaEvent> broadcaster)
    : TunaService.TunaServiceBase
{
    private static TunaMusicDTO? _lastState;
    private static bool _playerDataSourceRegistered;

    public override async Task Subscribe(
        SubscribeRequest request,
        IServerStreamWriter<TunaEvent> responseStream,
        ServerCallContext context
    )
    {
        using var subscription = broadcaster.Subscribe();

        if (_lastState is not null)
        {
            await broadcaster.BroadcastExceptAsync(
                subscription.Id,
                new TunaEvent { Info = TunaGrpcMapper.ToProto(_lastState) }
            );
        }

        await broadcaster.PumpAsync(subscription, responseStream, context.CancellationToken);
    }

    public override async Task<SendPlayerDataResponse> SendPlayerData(
        SendPlayerDataRequest request,
        ServerCallContext context
    )
    {
        _lastState = TunaGrpcMapper.ToDto(request.Info);

        await broadcaster.BroadcastExceptAsync(
            request.SubscriberId,
            new TunaEvent { Info = request.Info }
        );

        return new SendPlayerDataResponse();
    }

    public override Task<BeYmResponse> BeYm(BeYmRequest request, ServerCallContext context)
    {
        if (_playerDataSourceRegistered)
        {
            throw new RpcException(
                new Status(StatusCode.FailedPrecondition, "Источник данных уже зарегистрирован")
            );
        }

        _playerDataSourceRegistered = true;

        return Task.FromResult(new BeYmResponse());
    }
}
