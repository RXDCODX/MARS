using System.Text.Json;
using System.Text.Json.Serialization;

namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Запрос на выполнение команды, инициированный сервисом платформы.
/// Платформа сама детектит префикс команды и определяет, является ли вызывающий
/// администратором (правило живёт в сервисе платформы, а не в MARS.Commands) —
/// результат передаётся полем <see cref="IsAdmin"/>.
/// </summary>
/// <remarks>
/// Направление: <c>ИЗ MARS.Twitch => В MARS.Commands</c>. Публикуется
/// <c>TwitchCommandForwarderService</c>; потребляет <c>CommandRequestListener</c>
/// (MARS.Commands). Ответ доставляется обратно по REST (outbox): Commands зовёт
/// internal-эндпоинт сервиса платформы, адрес и идентификатор ответа лежат в
/// <see cref="ReplyTo"/> и <see cref="ResponseMode"/>.
/// </remarks>
public sealed class CommandExecuteRequestedEvent : IMarsEventMessage
{
    public CommandPlatform Platform { get; set; }

    public string CommandName { get; set; } = string.Empty;

    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// Права вызывающего, определённые сервисом платформы.
    /// </summary>
    public bool IsAdmin { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Адрес доставки ответа на платформе: имя канала (Twitch), id канала (Discord),
    /// id чата (Telegram). Пусто для Api-платформы.
    /// </summary>
    public string ReplyTo { get; set; } = string.Empty;

    /// <summary>
    /// Идентификатор исходного сообщения (Twitch message id, Discord message id,
    /// Telegram message id), если ответ должен быть привязан к нему.
    /// </summary>
    public string SourceMessageId { get; set; } = string.Empty;

    /// <summary>
    /// Для Telegram inline-режима — id inline-сообщения, текст которого надо
    /// отредактировать после выполнения команды.
    /// </summary>
    public string InlineMessageId { get; set; } = string.Empty;

    public CommandResponseMode ResponseMode { get; set; } = CommandResponseMode.Send;

    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");
}

/// <summary>
/// Платформа, с которой инициирована команда.
/// </summary>
public enum CommandPlatform
{
    Api = 0,
    Telegram = 1,
    Twitch = 2,
    Discord = 3,
}

/// <summary>
/// Способ доставки ответа на платформу.
/// </summary>
public enum CommandResponseMode
{
    /// <summary>Отправить новое сообщение в чат/канал.</summary>
    Send = 0,

    /// <summary>Отредактировать текст inline-сообщения (Telegram inline).</summary>
    EditInline = 1,
}
