using System.Text.Json;
using System.Text.Json.Serialization;

namespace MARS.Shared.Contracts.Messaging;

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
