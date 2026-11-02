using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using MARS.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Messaging;

/// <summary>
/// The single RabbitMQ consumer for Twitch EventSub notifications in an application.
/// It identifies the concrete EventSub payload model from <c>subscription_type</c>
/// and raises one asynchronous .NET event for the application.
/// </summary>
/// <remarks>
/// Each application registers exactly one instance and gives it a stable, app-specific
/// queue name. Different applications must use different queues to receive independent
/// copies of the topic stream. Replicas of the same application may share its queue.
///
/// Known Twitch subscription types are deserialized to the corresponding DTO in
/// <see cref="TwitchEventSubPayloads"/>. Unknown types are delivered as <see cref="JsonElement"/>
/// so a newer Twitch payload is not discarded. Applications may extend or override
/// the mapping with <see cref="RegisterEventType{TEvent}"/>.
///
/// Delivery is at-least-once: if an application event handler throws, RabbitMqConsumerBase
/// retries the delivery. Application handlers should be idempotent.
/// </remarks>
public sealed class TwitchEventSubConsumer : RabbitMqConsumerBase
{
    private static readonly JsonSerializerOptions EnvelopeSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions EventSerializerOptions =
        CreateEventSerializerOptions();

    private readonly ConcurrentDictionary<string, Type> _eventTypes = new(
        new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            [TwitchEventSubTypes.ChannelRaid] = typeof(TwitchChannelRaidEvent),
            [TwitchEventSubTypes.StreamOnline] = typeof(TwitchStreamOnlineEvent),
            [TwitchEventSubTypes.StreamOffline] = typeof(TwitchStreamOfflineEvent),
            [TwitchEventSubTypes.ChannelPointsCustomRewardRedemptionAdd] =
                typeof(TwitchChannelPointsCustomRewardRedemptionAddedEvent),
            [TwitchEventSubTypes.ChannelModeratorAdd] = typeof(TwitchChannelModeratorAddedEvent),
            [TwitchEventSubTypes.ChannelVipAdd] = typeof(TwitchChannelVipAddedEvent),
            [TwitchEventSubTypes.ChannelFollow] = typeof(TwitchChannelFollowEvent),
        }
    );

    public TwitchEventSubConsumer(
        IOptions<RabbitMqOptions> options,
        string serviceName,
        string queueName,
        ILogger<TwitchEventSubConsumer> logger
    )
        : base(
            options.Value,
            serviceName,
            queueName,
            [RabbitMqConfig.TwitchEventSubRaw],
            logger
        )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
    }

    /// <summary>
    /// Raised for every EventSub notification after automatic payload deserialization.
    /// Subscribe from the application using an async handler:
    /// <c>consumer.EventReceived += HandleEventAsync;</c>
    /// </summary>
    public event TwitchEventSubNotificationHandler? EventReceived;

    /// <summary>
    /// Register or replace the DTO used for a Twitch EventSub subscription type.
    /// Call during application startup, before the corresponding notification arrives.
    /// An unregistered type is exposed as raw <see cref="JsonElement"/>.
    /// </summary>
    public void RegisterEventType<TEvent>(string subscriptionType)
        where TEvent : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionType);
        _eventTypes[subscriptionType.Trim()] = typeof(TEvent);
    }

    protected override async Task HandleMessageAsync(
        string routingKey,
        string json,
        CancellationToken ct
    )
    {
        if (!string.Equals(routingKey, RabbitMqConfig.TwitchEventSubRaw, StringComparison.Ordinal))
        {
            return;
        }

        var envelope =
            JsonSerializer.Deserialize<TwitchEventSubEnvelope>(json, EnvelopeSerializerOptions)
            ?? throw new JsonException("Twitch EventSub envelope is empty.");

        // Raw forwarding also includes welcome, keepalive, reconnect, revocation and
        // other protocol frames. Only notification frames represent application events.
        if (!string.Equals(envelope.MessageType, "notification", StringComparison.Ordinal))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(envelope.SubscriptionType))
        {
            throw new JsonException("Twitch EventSub notification has no subscription_type.");
        }

        if (
            envelope.Raw.ValueKind != JsonValueKind.Object
            || !envelope.Raw.TryGetProperty("payload", out var payload)
            || payload.ValueKind != JsonValueKind.Object
            || !payload.TryGetProperty("event", out var eventElement)
            || eventElement.ValueKind != JsonValueKind.Object
        )
        {
            throw new JsonException(
                "Twitch EventSub notification has no raw.payload.event object."
            );
        }

        var knownType = _eventTypes.TryGetValue(envelope.SubscriptionType, out var eventClrType);
        var eventData = knownType
            ? eventElement.Deserialize(eventClrType!, EventSerializerOptions)
                ?? throw new JsonException(
                    $"Twitch EventSub payload for '{envelope.SubscriptionType}' deserialized to null."
                )
            : eventElement.Clone();

        var notification = new TwitchEventSubNotification(
            envelope.SchemaVersion,
            envelope.MessageId,
            envelope.MessageType,
            envelope.SubscriptionType,
            envelope.SubscriptionVersion,
            envelope.SubscriptionId,
            envelope.SessionId,
            envelope.EventName,
            envelope.ObservedAtUtc,
            envelope.SourceService,
            envelope.TraceParent,
            eventData,
            knownType,
            envelope.Raw.Clone(),
            eventElement.Clone()
        );

        await RaiseEventReceivedAsync(notification).ConfigureAwait(false);
    }

    private async Task RaiseEventReceivedAsync(TwitchEventSubNotification notification)
    {
        var handlers = EventReceived;
        if (handlers is null)
        {
            logger.LogWarning(
                "Twitch EventSub notification {SubscriptionType} received, but no EventReceived handlers are registered",
                notification.SubscriptionType
            );
            return;
        }

        List<Exception>? errors = null;

        // Await each async listener before RabbitMQ acknowledges the message.
        foreach (var callback in handlers.GetInvocationList())
        {
            try
            {
                await ((TwitchEventSubNotificationHandler)callback)(this, notification)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                (errors ??= []).Add(exception);
                logger.LogError(
                    exception,
                    "Twitch EventSub handler failed for {SubscriptionType} (message {MessageId})",
                    notification.SubscriptionType,
                    notification.MessageId
                );
            }
        }

        if (errors is { Count: 1 })
        {
            throw errors[0];
        }

        if (errors is { Count: > 1 })
        {
            throw new AggregateException(
                $"Multiple application handlers failed for Twitch EventSub '{notification.SubscriptionType}'.",
                errors
            );
        }
    }

    internal Task ProcessMessageAsync(
        string routingKey,
        string json,
        CancellationToken cancellationToken = default
    ) => HandleMessageAsync(routingKey, json, cancellationToken);

    private static JsonSerializerOptions CreateEventSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        return options;
    }
}
