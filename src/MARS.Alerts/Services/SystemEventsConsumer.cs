using MARS.Shared.Configuration;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using MARS.Shared.Telemetry;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services;

/// <summary>
/// Потребитель системных событий (<c>waifu.*</c>, <c>media.track.*</c>) в очереди
/// <c>alerts.system</c>.
/// Раньше был отдельной <c>BackgroundService</c> с собственным AMQP-соединением и
/// catch-and-log: любая ошибка молча терялась, а падение брокера навсегда убивало
/// потребителя. Теперь переподключение, retry и DLQ обеспечивает
/// <see cref="RabbitMqConsumerBase"/>.
/// </summary>
public class SystemEventsConsumer(
    IOptions<RabbitMqOptions> options,
    ITelegramusNotifier notifier,
    ILogger<SystemEventsConsumer> logger
)
    : RabbitMqConsumerBase(
        options.Value,
        "alerts",
        RabbitMqConfig.SystemEventsQueue,
        RabbitMqConfig.SystemRoutingKeys,
        logger
    )
{
    protected override async Task HandleMessageAsync(
        string routingKey,
        string json,
        CancellationToken ct
    )
    {
        switch (routingKey)
        {
            case RabbitMqConfig.WaifuRollResult:
                await notifier.Explosion();
                break;

            case RabbitMqConfig.FumoRollResult:
            case RabbitMqConfig.MikuRollResult:
            case RabbitMqConfig.FrogRollResult:
                MarsMetrics.RabbitMqConsumed.Add(
                    1,
                    new KeyValuePair<string, object?>("queue", RabbitMqConfig.SystemEventsQueue)
                );
                break;

            case RabbitMqConfig.TrackStarted:
            case RabbitMqConfig.TrackEnded:
            case RabbitMqConfig.TrackAdded:
                logger.LogDebug("Media event {RoutingKey} received", routingKey);
                break;

            default:
                logger.LogDebug("Unhandled routing key {RoutingKey}", routingKey);
                break;
        }

        await Task.CompletedTask;
    }
}
