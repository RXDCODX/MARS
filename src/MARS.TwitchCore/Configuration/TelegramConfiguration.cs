namespace MARS.TwitchCore.Configuration;

public class TelegramConfiguration
{
    public static readonly string TelegramSection = "Telegram";

    /// <summary>
    /// Адресаты уведомлений: <c>Telegram:AdminIds:0</c>, <c>:1</c> и так далее —
    /// то есть <c>Telegram__AdminIds__0</c> в переменных окружения.
    /// </summary>
    /// <remarks>
    /// Имя свойства совпадает с именем секции намеренно: конфигурационный биндер
    /// ищет секцию по имени свойства, и <c>AdminIdsArray</c> из <c>AdminIds</c>
    /// не читал бы ничего — список молча остался бы пустым, и уведомление ушло бы
    /// в никуда.
    /// </remarks>
    public long[] AdminIds { get; set; } = [];
}
