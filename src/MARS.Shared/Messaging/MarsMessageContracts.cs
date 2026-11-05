using System.Text.Json;
using Wolverine.Attributes;

namespace MARS.Shared.Messaging;

/// <summary>
/// Стабильные псевдонимы интеграционных сообщений.
/// Псевдоним используется как тип сообщения Wolverine и имя RabbitMQ exchange.
/// </summary>
public static class MarsMessageAliases
{
    public const string RewardRedeemed = "mars.reward.redeemed";
    public const string ChatMessageReceived = "mars.chat.message.received";
    public const string RewardInputMessage = "mars.chat.message.reward-input";
    public const string ChatMessageDeleted = "mars.chat.message.deleted";
    public const string ChatSend = "mars.chat.send";
    public const string EventSubRaw = "mars.eventsub.raw";
}

/// <summary>
/// Обычное сообщение чата. Отдельный тип не позволяет смешивать его с сообщением,
/// отправленным через Twitch-награду с вводом текста.
/// </summary>
[MessageIdentity(MarsMessageAliases.ChatMessageReceived)]
public sealed class ChatMessageReceivedEvent : ChatMessageEvent { }

/// <summary>
/// Сообщение, отправленное через Twitch-награду с вводом текста.
/// </summary>
[MessageIdentity(MarsMessageAliases.RewardInputMessage)]
public sealed class RewardInputMessageEvent : ChatMessageEvent { }

/// <summary>
/// Удаление сообщения в чате.
/// </summary>
[MessageIdentity(MarsMessageAliases.ChatMessageDeleted)]
public sealed class ChatMessageDeletedEvent
{
    public string MessageId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Сырой кадр EventSub. Контракт предназначен для типизированной передачи
/// уведомлений EventSub между сервисами без зависимости от Twitch transport DTO.
/// </summary>
[MessageIdentity(MarsMessageAliases.EventSubRaw)]
public sealed class EventSubRawEvent
{
    public int SchemaVersion { get; set; } = 1;
    public string? MessageId { get; set; }
    public string MessageType { get; set; } = "unknown";
    public string? SubscriptionType { get; set; }
    public string? SubscriptionVersion { get; set; }
    public string? SubscriptionId { get; set; }
    public string? SessionId { get; set; }
    public string EventName { get; set; } = "unknown";
    public DateTimeOffset ObservedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string SourceService { get; set; } = "MARS.TwitchCore";
    public string? TraceParent { get; set; }
    public JsonElement Raw { get; set; }
}
