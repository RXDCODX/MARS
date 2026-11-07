using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Platform.Web.AspNetCore.Infrastructure.Configuration;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services.Twitch.Messages;

/// <summary>
/// Независимый слушатель сырых EventSub-событий, пересылаемых MARS.TwitchCore.
/// Получает типизированный общий контракт, не зависящий от TwitchLib.
/// </summary>
public class TwitchEventSubListener(
    IOptions<RabbitMqOptions> options,
    ILogger<TwitchEventSubListener> logger
)
    : RabbitMqConsumerBase(
        options.Value,
        "alerts",
        RabbitMqConfig.AlertsTwitchEventSubQueue,
        [RabbitMqConfig.EventSubRaw],
        logger
    )
{
    protected override Task HandleMessageAsync(
        string routingKey,
        string json,
        CancellationToken ct
    )
    {
        var eventSubEvent = Deserialize<EventSubRawEvent>(json);

        if (eventSubEvent is null)
        {
            throw new InvalidOperationException(
                $"EventSub message on '{routingKey}' could not be deserialized."
            );
        }

        return OnEventSubReceivedAsync(eventSubEvent, ct);
    }

    /// <summary>Точка расширения для обработки входящего EventSub-фрейма.</summary>
    protected virtual Task OnEventSubReceivedAsync(EventSubRawEvent eventSubEvent, CancellationToken ct)
    {
        logger.LogDebug(
            "Twitch EventSub frame received: event={EventName}, subscription={SubscriptionType}, "
                + "messageType={MessageType}, messageId={MessageId}",
            eventSubEvent.EventName,
            eventSubEvent.SubscriptionType,
            eventSubEvent.MessageType,
            eventSubEvent.MessageId
        );

        return Task.CompletedTask;
    }
}
