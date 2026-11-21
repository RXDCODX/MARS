using System.Threading.Tasks;
using MARS.Shared.Contracts;

namespace MARS.Platform.Infrastructure.Http;

/// <summary>
/// Куда <see /> отдаёт собранное событие оверлея.
/// </summary>
/// <remarks>
/// Разделено намеренно. Построение <c>TelegramusEvent</c> из 35 методов
/// внутри процесса это широковещатель в памяти, между процессами — <c>Fire</c>.
/// Именно поэтому <c>Fire</c> и не оказался лишним: без него сервис, уже
/// публикующий события оверлея, не мог бы доставить их в сервис-владелец.
/// </remarks>
public interface ITelegramusEventSink
{
    Task DispatchAsync(TelegramusEvent notification);
}
