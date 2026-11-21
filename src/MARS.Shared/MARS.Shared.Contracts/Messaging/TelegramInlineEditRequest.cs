namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Запрос на редактирование текста inline-сообщения Telegram (inline-режим команд).
/// </summary>
public sealed class TelegramInlineEditRequest
{
    public string InlineMessageId { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}
