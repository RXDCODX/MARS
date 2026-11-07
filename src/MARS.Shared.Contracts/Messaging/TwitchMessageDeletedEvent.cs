namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Событие удаления сообщения Twitch, публикуемое MARS.TwitchCore в RabbitMQ.
/// </summary>
public sealed class TwitchMessageDeletedEvent : IMarsEventMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}
