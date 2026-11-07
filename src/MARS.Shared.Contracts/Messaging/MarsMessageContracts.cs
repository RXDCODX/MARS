using System.Text.Json;

namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Сырой кадр EventSub. Контракт предназначен для типизированной передачи
/// уведомлений EventSub между сервисами без зависимости от Twitch transport DTO.
/// </summary>
public sealed class EventSubRawEvent : IMarsEventMessage
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

/// <summary>
/// Сообщение чата удалено модератором или ботом. Публикует MARS.TwitchCore.
/// </summary>
public sealed class ChatMessageDeletedEvent : IMarsEventMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}
