namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Запрос на отправку сообщения в чат Twitch. Обрабатывает MARS.Twitch.Api —
/// единственный владелец IRC/EventSub-подключения.
/// </summary>
public sealed class TwitchChatSendRequest
{
    public string Channel { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? ReplyTo { get; set; }
}
