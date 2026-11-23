using System.Text.Json;

namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Сырой кадр EventSub. Контракт предназначен для типизированной передачи
/// уведомлений EventSub между сервисами без зависимости от Twitch transport DTO.
/// </summary>
/// <remarks>
/// Направление: <c>ИЗ MARS.Twitch => В MARS.Alerts</c>. Публикуется
/// <c>EventSubRawForwarder</c> (каждый сырой фрейм websocket'а); потребляет
/// <c>TwitchEventSubListener</c> (MARS.Alerts). Источник сервиса —
/// <c>MARS.Twitch</c> (ранее «MARS.TwitchCore»).
/// </remarks>
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
    public string SourceService { get; set; } = "MARS.Twitch";
    public string? TraceParent { get; set; }
    public JsonElement Raw { get; set; }
}
