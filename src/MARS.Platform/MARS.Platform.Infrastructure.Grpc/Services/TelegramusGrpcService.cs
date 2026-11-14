using Grpc.Core;
using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Grpc.Extensions;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Logging;

namespace MARS.Platform.Infrastructure.Grpc.Services;

public sealed class TelegramusGrpcService(
    EventBroadcaster<TelegramusEvent> broadcaster,
    ILogger<TelegramusGrpcService> logger
) : TelegramusService.TelegramusServiceBase
{
    public override async Task Subscribe(
        SubscribeRequest request,
        IServerStreamWriter<TelegramusEvent> responseStream,
        ServerCallContext context
    )
    {
        using var subscription = broadcaster.Subscribe(request.SubscriberId);

        logger.LogInformation(
            "gRPC client subscribed to Telegramus events: {SubscriberId}",
            subscription.Id
        );

        await subscription.PumpAsync(responseStream, context.CancellationToken);
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

    /// <summary>
    /// Вброс события подписчикам этого процесса. Нужен для вызовов извне:
    /// <see cref="EventBroadcaster{TMessage}"/> живёт в памяти сервиса, и без
    /// этого unary-метода ни один сервис не может вызвать оверлей другого —
    /// <see cref="ITelegramusNotifier"/> умеет писать только в свой broadcaster.
    /// </summary>
    public override async Task<FireResponse> Fire(FireRequest request, ServerCallContext context)
    {
        await broadcaster.BroadcastAsync(request.Event);

        logger.LogInformation(
            "Fire: событие {EventCase} разослано {Subscribers} подписчикам",
            request.Event.EventCase,
            broadcaster.SubscriberCount
        );

        return new FireResponse();
    }
}
