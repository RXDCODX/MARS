namespace MARS.Shared.Grpc.EventSub;

/// <summary>
/// Типизированный клиент управления подписками EventSub.
/// Скрывает детали <see cref="Grpc.Core.AsyncUnaryCall{TResponse}"/> от вызывающих
/// сервисов и оставляет им обычный async API с поддержкой отмены.
/// </summary>
public sealed class EventSubSubscriptionClient(
    EventSubSubscriptionService.EventSubSubscriptionServiceClient client
) : IEventSubSubscriptionClient
{
    public async Task<CreateEventSubSubscriptionReply> CreateSubscriptionAsync(
        CreateEventSubSubscriptionRequest request,
        CancellationToken cancellationToken = default
    )
    {
        using var call = client.CreateSubscriptionAsync(
            request,
            cancellationToken: cancellationToken
        );

        return await call.ResponseAsync.ConfigureAwait(false);
    }

    public async Task<ListEventSubSubscriptionsReply> ListSubscriptionsAsync(
        ListEventSubSubscriptionsRequest request,
        CancellationToken cancellationToken = default
    )
    {
        using var call = client.ListSubscriptionsAsync(
            request,
            cancellationToken: cancellationToken
        );

        return await call.ResponseAsync.ConfigureAwait(false);
    }

    public async Task<DeleteEventSubSubscriptionReply> DeleteSubscriptionAsync(
        DeleteEventSubSubscriptionRequest request,
        CancellationToken cancellationToken = default
    )
    {
        using var call = client.DeleteSubscriptionAsync(
            request,
            cancellationToken: cancellationToken
        );

        return await call.ResponseAsync.ConfigureAwait(false);
    }
}
