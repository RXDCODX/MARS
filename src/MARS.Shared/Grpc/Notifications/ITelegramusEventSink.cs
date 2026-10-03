using MARS.Shared.Grpc.Telegramus;

namespace MARS.Shared.Grpc.Notifications;

/// <summary>
/// Куда <see cref="TelegramusNotifier"/> отдаёт собранное событие оверлея.
/// </summary>
/// <remarks>
/// Разделено намеренно. Построение <c>TelegramusEvent</c> из 35 методов
/// <c>ITelegramusNotifier</c> одинаково для обоих транспортов, а доставка разная:
/// внутри процесса это широковещатель в памяти, между процессами — <c>Fire</c>.
/// Именно поэтому <c>Fire</c> и не оказался лишним: без него сервис, уже
/// публикующий события оверлея, не мог бы доставить их в сервис-владелец.
/// </remarks>
public interface ITelegramusEventSink
{
    Task DispatchAsync(TelegramusEvent notification);
}
