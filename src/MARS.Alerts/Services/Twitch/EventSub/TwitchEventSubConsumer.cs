using System.Collections.Concurrent;
using System.Text.Json;
using MARS.Alerts.Services.Twitch.Rewards.Activation;
using MARS.Shared.Configuration;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services.Twitch.EventSub;

/// <summary>
/// Прозрачно передаёт Twitch EventSub notifications в единый канал Telegramus.
/// Channel Points обрабатывается здесь же: reward processor добавляет MARS
/// extension к исходному JSON и не использует отдельную очередь или Hub-метод.
/// </summary>
public sealed class TwitchEventSubConsumer(
    IOptions<RabbitMqOptions> options,
    ITelegramusNotifier notifier,
    IRewardRedemptionProcessor rewardProcessor,
    ILogger<TwitchEventSubConsumer> logger
)
    : RabbitMqConsumerBase(
        options.Value,
        "alerts",
        QueueName,
        [RabbitMqConfig.RoutingKeys.EventSubReceived],
        logger
    )
{
    public const string QueueName = "alerts.twitch.eventsub";

    private const string RedemptionSubscriptionType =
        "channel.channel_points_custom_reward_redemption.add";

    private readonly ConcurrentDictionary<string, byte> _processingMessages = new(
        StringComparer.Ordinal
    );

    private readonly ConcurrentDictionary<string, DateTimeOffset> _processedMessages = new(
        StringComparer.Ordinal
    );

    protected override async Task HandleMessageAsync(
        string routingKey,
        string json,
        CancellationToken cancellationToken
    )
    {
        using var envelope = JsonDocument.Parse(json);
        var root = envelope.RootElement;
        var messageType = ReadString(root, "messageType", "MessageType");

        // session_welcome/keepalive/reconnect/revocation — сообщения протокола,
        // а не пользовательские события оверлея.
        if (!string.Equals(messageType, "notification", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (
            !TryGetProperty(root, out var raw, "raw", "Raw")
            || raw.ValueKind != JsonValueKind.Object
        )
        {
            throw new InvalidOperationException(
                $"EventSub envelope on '{routingKey}' does not contain an object raw payload"
            );
        }

        var metadata = raw.TryGetProperty("metadata", out var value) ? value : default;
        var messageId = ReadString(metadata, "message_id");
        var subscriptionType = ReadString(metadata, "subscription_type");
        var isRedemption = string.Equals(
            subscriptionType,
            RedemptionSubscriptionType,
            StringComparison.Ordinal
        );

        if (string.IsNullOrWhiteSpace(messageId))
        {
            var outgoing = isRedemption
                ? await ProcessRedemptionAsync(root, raw, cancellationToken)
                : raw.GetRawText();

            if (!string.IsNullOrWhiteSpace(outgoing))
            {
                await notifier.TwitchEventSub(outgoing);
            }

            return;
        }

        PruneProcessedMessages();

        if (_processedMessages.ContainsKey(messageId) || !_processingMessages.TryAdd(messageId, 0))
        {
            logger.LogDebug("Duplicate EventSub message {MessageId} ignored", messageId);
            return;
        }

        // Закрываем гонку: предыдущая обработка могла завершиться между первой
        // проверкой completed и регистрацией текущего in-flight маркера.
        if (_processedMessages.ContainsKey(messageId))
        {
            _processingMessages.TryRemove(messageId, out _);
            logger.LogDebug("Duplicate EventSub message {MessageId} ignored", messageId);
            return;
        }

        try
        {
            var outgoing = isRedemption
                ? await ProcessRedemptionAsync(root, raw, cancellationToken)
                : raw.GetRawText();

            // null означает осознанно подавленный показ (например, RickRoll).
            // Событие считаем обработанным, чтобы повторная доставка не повторила
            // побочный эффект.
            if (!string.IsNullOrWhiteSpace(outgoing))
            {
                await notifier.TwitchEventSub(outgoing);
            }

            _processedMessages[messageId] = DateTimeOffset.UtcNow;
        }
        finally
        {
            _processingMessages.TryRemove(messageId, out _);
        }
    }

    private async Task<string?> ProcessRedemptionAsync(
        JsonElement envelope,
        JsonElement raw,
        CancellationToken cancellationToken
    )
    {
        string? alertKey = null;
        Guid? mediaInfoId = null;

        if (
            TryGetProperty(envelope, out var rewardMetadata, "rewardMetadata")
            || TryGetProperty(envelope, out rewardMetadata, "RewardMetadata")
        )
        {
            alertKey = ReadString(rewardMetadata, "alertKey", "AlertKey");

            var mediaId = ReadString(rewardMetadata, "mediaInfoId", "MediaInfoId");
            if (Guid.TryParse(mediaId, out var parsedMediaId) && parsedMediaId != Guid.Empty)
            {
                mediaInfoId = parsedMediaId;
            }
        }

        return await rewardProcessor.ProcessAsync(
            raw.GetRawText(),
            alertKey,
            mediaInfoId,
            cancellationToken
        );
    }

    private void PruneProcessedMessages()
    {
        if (_processedMessages.Count < 10_000)
        {
            return;
        }

        var cutoff = DateTimeOffset.UtcNow.AddHours(-24);
        foreach (var entry in _processedMessages)
        {
            if (entry.Value < cutoff)
            {
                _processedMessages.TryRemove(entry.Key, out _);
            }
        }
    }

    private static string? ReadString(JsonElement element, params string[] names)
    {
        return
            TryGetProperty(element, out var property, names)
            && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static bool TryGetProperty(
        JsonElement element,
        out JsonElement value,
        params string[] names
    )
    {
        foreach (var name in names)
        {
            if (
                element.ValueKind == JsonValueKind.Object
                && element.TryGetProperty(name, out value)
            )
            {
                return true;
            }
        }

        value = default;
        return false;
    }
}
