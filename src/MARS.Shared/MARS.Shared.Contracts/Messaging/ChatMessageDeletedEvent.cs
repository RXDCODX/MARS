using System.Text.Json;

namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Сообщение чата удалено модератором или ботом. Публикует MARS.TwitchCore.
/// </summary>
public sealed class ChatMessageDeletedEvent : IMarsEventMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}
