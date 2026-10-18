namespace MARS.Telegram.Configuration;

public class TelegramConfiguration
{
    public static readonly string TelegramSection = "Telegram";

    /// <summary>
    /// Идентификаторы администраторов, которым сервис шлёт сообщения.
    /// </summary>
    /// <remarks>
    /// Имя свойства — это и есть имя секции в конфигурации: биндер ищет
    /// <c>Telegram:AdminIds</c>, то есть <c>Telegram__AdminIds__0</c> в записи
    /// переменной окружения. Прежнее имя <c>AdminIdsArray</c> ждало секцию
    /// <c>Telegram:AdminIdsArray</c>, а compose передаёт <c>Telegram__AdminIds__0</c>,
    /// и список молча приходил пустым — слать было некому.
    /// </remarks>
    public long[] AdminIds { get; set; } = [];
}
