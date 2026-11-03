using Grpc.Core;
using MARS.Shared.Grpc.Telegramus;
using Microsoft.Extensions.Logging;

namespace MARS.Shared.Grpc.Notifications;

/// <summary>
/// Доставка события оверлея в сервис-владелец через unary-вызов <c>Fire</c>.
/// </summary>
/// <remarks>
/// Нужен сервисам, которые публикуют события оверлея, но не владеют им.
/// Широковещатель лежит в памяти <c>MARS.Alerts</c>, поэтому из другого
/// процесса доставить событие можно только вызовом наружу.
/// </remarks>
public sealed class GrpcTelegramusEventSink(
    TelegramusService.TelegramusServiceClient client,
    ILogger<GrpcTelegramusEventSink> logger
) : ITelegramusEventSink
{
    public async Task DispatchAsync(TelegramusEvent notification)
    {
        // Ошибка доставки не должна ронять вызывающий сервис: событие оверлея —
        // побочный эффект (например, RewardAlertConsumer), а потеря подписчика
        // не повод перезапускать консьюмера в ретрай и DLQ. Поэтому исключение
        // гасится с записью в журнал, а не пробрасывается.
        try
        {
            await client.FireAsync(new FireRequest { Event = notification });
        }
        catch (RpcException ex)
        {
            logger.LogError(
                ex,
                "Не удалось доставить событие оверлея {EventCase} в MARS.Alerts",
                notification.EventCase
            );
        }
    }
}
