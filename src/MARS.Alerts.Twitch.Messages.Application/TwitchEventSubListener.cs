using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application;

/// <summary>Wolverine listener сырых EventSub-фреймов, пересылаемых MARS.TwitchCore.</summary>
public class TwitchEventSubListener(ILogger<TwitchEventSubListener> logger)
    : MarsEventListener<EventSubRawEvent>
{
    public override Task Handle(
        EventSubRawEvent eventSubEvent,
        CancellationToken cancellationToken
    ) => OnEventSubReceivedAsync(eventSubEvent, cancellationToken);

    /// <summary>Точка расширения для последующей обработки EventSub-события.</summary>
    protected virtual Task OnEventSubReceivedAsync(
        EventSubRawEvent eventSubEvent,
        CancellationToken ct
    )
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
