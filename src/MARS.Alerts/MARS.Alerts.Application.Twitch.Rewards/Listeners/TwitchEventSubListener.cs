using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Wolverine listener сырых EventSub-фреймов, пересылаемых MARS.TwitchCore.
/// Передаёт входящий Twitch event всем подписчикам RewardHandlerBase.
/// </summary>
public sealed class TwitchEventSubListener(
    RewardEventManager eventManager,
    ILogger<TwitchEventSubListener> logger
) : MarsEventListener<EventSubRawEvent>
{
    public override Task Handle(EventSubRawEvent eventSubEvent, CancellationToken cancellationToken)
    {
        if (eventSubEvent is null)
        {
            logger.LogWarning("Twitch EventSub frame could not be deserialized, skipped");
            return Task.CompletedTask;
        }

        logger.LogDebug(
            "Twitch EventSub frame received: event={EventName}, subscription={SubscriptionType}, "
                + "messageType={MessageType}, messageId={MessageId}",
            eventSubEvent.EventName,
            eventSubEvent.SubscriptionType,
            eventSubEvent.MessageType,
            eventSubEvent.MessageId
        );

        return eventManager.HandleTwitchEventAsync(eventSubEvent, cancellationToken);
    }
}
