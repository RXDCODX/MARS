using System.Text.Json;
using System.Text.Json.Serialization;
using MARS.Shared.Configuration;
using Microsoft.Extensions.Logging;

namespace MARS.Shared.Messaging;

/// <summary>
/// RabbitMQ base for independently subscribing to Twitch EventSub notifications.
///
/// Each logical subscriber must use its own durable queue name. Queues bound to
/// <see cref="RabbitMqConfig.TwitchEventSubRaw"/> each receive a copy of the stream;
/// replicas of the same logical subscriber may share that queue and compete for work.
/// Pass one or more Twitch subscription type identifiers to filter the notifications.
/// With no filters, every notification is deserialized as TEvent.
/// Non-notification WebSocket frames (welcome, keepalive, reconnect, revocation, etc.)
/// are intentionally skipped by this typed consumer; the original frame remains
/// available in the raw RabbitMQ stream.
/// </summary>
/// <typeparam name="TEvent">DTO matching the JSON object at raw.payload.event.</typeparam>
public abstract class TwitchEventSubConsumer<TEvent> : RabbitMqConsumerBase
    where TEvent : notnull
{
    private static readonly JsonSerializerOptions EnvelopeSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions EventSerializerOptions =
        CreateEventSerializerOptions();

    private readonly HashSet<string> _subscriptionTypes;

    /// <param name="options">Shared RabbitMQ connection options.</param>
    /// <param name="serviceName">Stable service name used in connection diagnostics.</param>
    /// <param name="queueName">
    /// Stable queue name unique to this logical subscriber. Use the same name across replicas.
    /// </param>
    /// <param name="logger">Consumer logger.</param>
    /// <param name="subscriptionTypes">
    /// Optional Twitch EventSub subscription types. Empty means all notification types.
    /// </param>
    protected TwitchEventSubConsumer(
        RabbitMqOptions options,
        string serviceName,
        string queueName,
        ILogger logger,
        params string[] subscriptionTypes
    )
        : base(options, serviceName, queueName, [RabbitMqConfig.TwitchEventSubRaw], logger)
    {
        ArgumentNullException.ThrowIfNull(subscriptionTypes);

        if (subscriptionTypes.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Twitch EventSub subscription type filters must not be empty.",
                nameof(subscriptionTypes)
            );
        }

        _subscriptionTypes = new HashSet<string>(
            subscriptionTypes.Select(subscriptionType => subscriptionType.Trim()),
            StringComparer.Ordinal
        );
    }

    /// <summary>
    /// Handles a strongly typed Twitch notification. The cancellation token follows
    /// RabbitMqConsumerBase's message-handler contract.
    /// </summary>
    protected abstract Task HandleEventAsync(
        TEvent twitchEvent,
        TwitchEventSubContext context,
        CancellationToken cancellationToken
    );

    protected sealed override async Task HandleMessageAsync(
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

        // The raw stream also includes WebSocket lifecycle/control frames. They do not
        // contain a notification event and are deliberately not delivered to typed handlers.
        if (!string.Equals(envelope.MessageType, "notification", StringComparison.Ordinal))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(envelope.SubscriptionType))
        {
            throw new JsonException("Twitch EventSub notification has no subscription_type.");
        }

        if (_subscriptionTypes.Count > 0 && !_subscriptionTypes.Contains(envelope.SubscriptionType))
        {
            return;
        }

        if (
            envelope.Raw.ValueKind != JsonValueKind.Object
            || !envelope.Raw.TryGetProperty("payload", out var payload)
            || payload.ValueKind != JsonValueKind.Object
            || !payload.TryGetProperty("event", out var eventElement)
        )
        {
            throw new JsonException(
                "Twitch EventSub notification has no raw.payload.event object."
            );
        }

        var twitchEvent = eventElement.Deserialize<TEvent>(EventSerializerOptions);

        if (twitchEvent is null)
        {
            throw new JsonException(
                $"Twitch EventSub payload for '{envelope.SubscriptionType}' deserialized to null."
            );
        }

        var context = new TwitchEventSubContext(
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
            envelope.Raw,
            eventElement
        );

        await HandleEventAsync(twitchEvent, context, ct).ConfigureAwait(false);
    }

    private static JsonSerializerOptions CreateEventSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            // Twitch EventSub event fields use snake_case, while consumer DTOs normally
            // use PascalCase. This lets consumers define ordinary C# DTOs without
            // annotating every property with JsonPropertyName.
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        return options;
    }
}
