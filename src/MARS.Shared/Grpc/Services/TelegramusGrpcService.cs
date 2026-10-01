using Grpc.Core;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Telegramus;
using Microsoft.Extensions.Logging;

namespace MARS.Shared.Grpc.Services;

public sealed class TelegramusGrpcService(
    GrpcEventBroadcaster<TelegramusEvent> broadcaster,
    ILogger<TelegramusGrpcService> logger
) : TelegramusService.TelegramusServiceBase
{
    public override async Task Subscribe(
        SubscribeRequest request,
        IServerStreamWriter<TelegramusEvent> responseStream,
        ServerCallContext context
    )
    {
        using var subscription = broadcaster.Subscribe();

        logger.LogInformation(
            "gRPC client subscribed to Telegramus events: {SubscriberId}",
            subscription.Id
        );

        await broadcaster.PumpAsync(subscription, responseStream, context.CancellationToken);
    }

    public override Task<LogErrorResponse> LogError(
        LogErrorRequest request,
        ServerCallContext context
    )
    {
        logger.LogError("Client error: {ErrorMessage}", request.ErrorMessage);

        return Task.FromResult(new LogErrorResponse());
    }

    public override Task<TwitchMsgResponse> TwitchMsg(
        TwitchMsgRequest request,
        ServerCallContext context
    )
    {
        logger.LogInformation("TwitchMsg received: {Message}", request.Msg);

        return Task.FromResult(new TwitchMsgResponse());
    }
}
