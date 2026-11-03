using System.Text.Json;
using MARS.Shared.Configuration;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services.Twitch.EventSub;

/// <summary>
/// Прозрачно передаёт уведомления Twitch EventSub в единый канал Telegramus.
/// </summary>
/// <remarks>
/// Исходный объект <c>raw</c> берётся из конверта EventSubRawForwarder через
/// <see cref="JsonElement.GetRawText"/>. Ни один payload не десериализуется в
/// TwitchLib DTO и неизвестные subscription_type не требуют регистрации.
/// Channel Points redemptions обрабатывает RewardActivatedConsumer: он
/// обогащает событие данными награды и серверного эффекта, но использует тот же
/// метод TelegramusHub.
/// </remarks>
public sealed class TwitchEventSubConsumer(
    IOptions<RabbitMqOptions> options,
    ITelegramusNotifier notifier,
    ILogger<TwitchEventSubConsumer> logger
)
    : RabbitMqConsumerBase(
        options.Value,
        "alerts",
        QueueName,
        [RabbitMqConfig.TwitchEventSubRaw],
        logger
    )
{
    public const string QueueName = "alerts.twitch.eventsub";

    private const string RedemptionSubscriptionType =
        "channel.channel_points_custom_reward_redemption.add";

    protected override async Task HandleMessageAsync(
        string routingKey,
        string json,
        CancellationToken cancellationToken
    )
    {
        using var envelope = JsonDocument.Parse(json);
        var root = envelope.RootElement;

        var messageType = ReadString(root, "messageType", "MessageType");

        // session_welcome/keepalive/reconnect/disconnect/revocation — сообщения
        // протокола WebSocket, а не пользовательские события оверлея.
        if (!string.Equals(messageType, "notification", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!TryGetProperty(root, out var raw, "raw", "Raw") || raw.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"EventSub envelope on '{routingKey}' does not contain an object raw payload"
            );
        }

        var metadata = raw.TryGetProperty("metadata", out var value)
            ? value
            : default;

        var subscriptionType = ReadString(metadata, "subscription_type");

        // Редемпшен идёт через тот же универсальный Hub-метод, но сначала
        // обрабатывается RewardActivatedConsumer: он добавляет медиа и данные
        // эффекта, чтобы обычная награда и специальный эффект не пришли дважды.
        if (string.Equals(
            subscriptionType,
            RedemptionSubscriptionType,
            StringComparison.Ordinal
        ))
        {
            return;
        }

        await notifier.TwitchEventSub(raw.GetRawText());
    }

    private static string? ReadString(JsonElement element, params string[] names)
    {
        return TryGetProperty(element, out var property, names)
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
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }
}
