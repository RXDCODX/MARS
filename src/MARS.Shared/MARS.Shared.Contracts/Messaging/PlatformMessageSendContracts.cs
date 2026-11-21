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

/// <summary>
/// Запрос на отправку сообщения в канал Discord.
/// </summary>
public sealed class DiscordMessageSendRequest
{
    public ulong ChannelId { get; set; }

    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Запрос на отправку сообщения в чат Telegram (через MTProto-бота).
/// </summary>
public sealed class TelegramMessageSendRequest
{
    public long ChatId { get; set; }

    public string Text { get; set; } = string.Empty;

    public long? ReplyToMessageId { get; set; }
}

/// <summary>
/// Запрос на редактирование текста inline-сообщения Telegram (inline-режим команд).
/// </summary>
public sealed class TelegramInlineEditRequest
{
    public string InlineMessageId { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}
