using System.Text.Json.Serialization;

namespace MARS.Telegram.Entities;

public class TelegramDiscordBindingCreateRequest
{
    public long TelegramChannelId { get; set; }

    /// <summary>
    /// Discord-идентификатор приходит и уходит строкой.
    /// </summary>
    /// <remarks>
    /// Snowflake не помещается в <c>Number</c> браузера: 64 бита против 53,
    /// и отправка числом молча округляла бы идентификатор — привязка велась бы
    /// к соседнему каналу. Ответ приходит строкой по той же причине, и теперь
    /// запрос симметричен ответу.
    /// <para>
    /// Флагов два, а не один. <c>WriteAsString</c> разрешает только записать
    /// число строкой, читать из строки он не разрешает: запрос со значением в
    /// кавычках отклонялся бы кодом 400 ещё до входа в контроллер, и страница
    /// создания связи была бы нерабочей. Поэтому <c>AllowReadingFromString</c>
    /// добавлен явно.
    /// </para>
    /// </remarks>
    [JsonNumberHandling(
        JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString
    )]
    public ulong DiscordChannelId { get; set; }
}
