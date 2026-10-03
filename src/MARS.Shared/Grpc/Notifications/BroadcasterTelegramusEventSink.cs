using MARS.Shared.Grpc.Telegramus;
using MARS.Shared.Telemetry;

namespace MARS.Shared.Grpc.Notifications;

/// <summary>
/// Внутрипроцессная доставка: событие уходит подписчикам широковещателя
/// в памяти этого же сервиса.
/// </summary>
/// <remarks>
/// Это приёмник сервиса-владельца оверлея. Он единственный, кто держит
/// широковещатель: <c>GrpcEventBroadcaster</c> живёт в памяти процесса, и
/// второй экземпляр в соседнем сервисе получал бы свои события в никуда.
/// </remarks>
public sealed class BroadcasterTelegramusEventSink(
    GrpcEventBroadcaster<TelegramusEvent> broadcaster
) : ITelegramusEventSink
{
    public Task DispatchAsync(TelegramusEvent notification)
    {
        using var activity = MarsActivities.StartGrpcBroadcast(nameof(TelegramusEvent));

        return broadcaster.BroadcastAsync(notification);
    }
}
