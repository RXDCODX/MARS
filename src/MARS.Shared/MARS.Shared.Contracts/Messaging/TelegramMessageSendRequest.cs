namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Запрос на отправку сообщения в чат Telegram (через MTProto-бота).
/// </summary>
public sealed class TelegramMessageSendRequest
{
    public long ChatId { get; set; }

    public string Text { get; set; } = string.Empty;

    public long? ReplyToMessageId { get; set; }
}
