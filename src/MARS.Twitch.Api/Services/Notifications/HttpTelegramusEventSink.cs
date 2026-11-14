using System;
using System.Threading.Tasks;
using Google.Protobuf;
using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Platform.Infrastructure.Grpc.Notifications;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Logging;

namespace MARS.Twitch.Api.Services.Notifications;

/// <summary>
/// Доставка события оверлея в сервис-владелец по HTTP.
/// </summary>
/// <remarks>
/// Нужен сервисам, которые публикуют события оверлея, но не владеют им.
/// Широковещатель лежит в памяти <c>MARS.Alerts</c>, поэтому из другого
/// процесса доставить событие можно только запросом наружу. Тело запроса —
/// JSON protobuf-сообщения: ветка <c>oneof</c> насчитывает 35 вариантов, и
/// повторять её вторым контрактом значило бы поддерживать две схемы одного
/// события.
/// </remarks>
public sealed class HttpTelegramusEventSink(
    IOverlayEventsClient client,
    ILogger<HttpTelegramusEventSink> logger
) : ITelegramusEventSink
{
    public async Task DispatchAsync(TelegramusEvent notification)
    {
        try
        {
            var payload = JsonFormatter.Default.Format(notification);

            if (!await client.FireAsync(payload))
            {
                logger.LogWarning(
                    "Событие оверлея {EventCase} не доставлено в MARS.Alerts",
                    notification.EventCase
                );
            }
        }
        catch (Exception exception)
        {
            // Ошибка доставки не должна ронять вызывающий сервис: событие оверлея —
            // побочный эффект (например, RewardAlertConsumer), а потеря подписчика
            // не повод перезапускать консьюмера в ретрай и DLQ.
            logger.LogError(
                exception,
                "Событие оверлея {EventCase} не доставлено в MARS.Alerts",
                notification.EventCase
            );
        }
    }
}
