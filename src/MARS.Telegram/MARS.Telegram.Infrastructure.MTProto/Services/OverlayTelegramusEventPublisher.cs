using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Telegram.Application.Services;
using Microsoft.Extensions.Logging;

namespace MARS.Telegram.Infrastructure.MTProto.Services;

/// <summary>
/// Публикация события оверлея через HTTP-клиент MARS.Alerts.
/// </summary>
public sealed class OverlayTelegramusEventPublisher(
    IOverlayEventsClient client,
    ILogger<OverlayTelegramusEventPublisher> logger
) : ITelegramusEventPublisher
{
    public async Task PublishAsync(string eventJson, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await client.FireAsync(eventJson, cancellationToken))
            {
                logger.LogWarning("Событие оверлея не доставлено в MARS.Alerts");
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Событие оверлея не доставлено в MARS.Alerts");
        }
    }
}
