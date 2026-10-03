using System.Text.Json.Serialization;

namespace MARS.Telegram.Entities;

public class TelegramDiscordBindingDto
{
    public Guid Id { get; set; }
    public long TelegramChannelId { get; set; }

    /// <summary>
    /// Discord-идентификатор приходит и уходит строкой.
    /// </summary>
    /// <remarks>
    /// Snowflake не помещается в <c>Number</c> браузера: 64 бита против 53,
    /// и клиент, прочитавший его числом, получил бы соседний идентификатор.
    /// <para>
    /// <c>AllowReadingFromString</c> добавлен к <c>WriteAsString</c>, чтобы тип
    /// принимал обе формы. Одного <c>WriteAsString</c> достаточно для ответа,
    /// но не для разбора: значение, записанное строкой, не читалось обратно, и
    /// любой потребитель DTO — в том числе тест — получал исключение разбора.
    /// </para>
    /// </remarks>
    [JsonNumberHandling(
        JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString
    )]
    public ulong DiscordChannelId { get; set; }

    public bool IsEnabled { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
