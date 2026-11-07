using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Запрос на отправку сообщения в чат. Публикуют сервисы, у которых нет собственного
/// IRC-подключения; обрабатывает MARS.TwitchCore — владелец единственного
/// подключения бот-аккаунта.
/// </summary>
public sealed class ChatSendEvent : IMarsEventMessage
{
    public string Channel { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ReplyTo { get; set; }
}
