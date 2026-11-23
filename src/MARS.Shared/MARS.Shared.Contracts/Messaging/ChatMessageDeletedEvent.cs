using System.Text.Json;

namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Сообщение чата удалено модератором или ботом.
/// </summary>
/// <remarks>
/// Направление: <c>ИЗ MARS.Twitch => В MARS.Alerts</c>. Публикуется
/// <c>TwitchMessagesPublisher</c>; потребляют <c>TwitchMessageDeletedListener</c>
/// и <c>OverlayChatMessageDeletedListener</c> (MARS.Alerts).
/// </remarks>
public sealed class ChatMessageDeletedEvent : IMarsEventMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}
