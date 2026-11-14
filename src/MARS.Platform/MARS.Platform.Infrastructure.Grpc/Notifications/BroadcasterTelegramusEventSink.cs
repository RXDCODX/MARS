using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Observability.Telemetry;
using MARS.Shared.Contracts.Telegramus;

namespace MARS.Platform.Infrastructure.Grpc.Notifications;

/// <summary>
/// Внутрипроцессная доставка: событие уходит подписчикам широковещателя
/// в памяти этого же сервиса.
/// </summary>
/// <remarks>
/// Это приёмник сервиса-владельца оверлея. Он единственный, кто держит
/// широковещатель: <c>EventBroadcaster</c> живёт в памяти процесса, и
/// второй экземпляр в соседнем сервисе получал бы свои события в никуда.
/// </remarks>
public sealed class BroadcasterTelegramusEventSink(EventBroadcaster<TelegramusEvent> broadcaster)
    : ITelegramusEventSink
{
    public Task DispatchAsync(TelegramusEvent notification)
    {
        using var activity = MarsActivities.StartGrpcBroadcast(nameof(TelegramusEvent));

        return broadcaster.BroadcastAsync(notification);
    }
}
