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
    /// <summary>Токен бота. Секрет.</summary>
    public string BotToken { get; set; } = string.Empty;

    public long[] AdminIds { get; set; } = [];

    /// <summary>
    /// Идентификатор приложения с my.telegram.org (<c>api_id</c>).
    /// </summary>
    /// <remarks>
    /// Обязателен не только пользовательскому клиенту, но и боту: клиент
    /// авторизуется по MTProto, а не через Bot API. Без него логин не проходит,
    /// и сервис молчит, не получая обновлений.
    /// </remarks>
    public string ApiId { get; set; } = string.Empty;

    /// <summary>Хеш приложения с my.telegram.org (<c>api_hash</c>). Секрет.</summary>
    public string ApiHash { get; set; } = string.Empty;

    /// <summary>
    /// Телефон пользовательского аккаунта. Нужен только клиенту WTelegram: боту
    /// хватает токена. Секрет.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Пароль двухфакторной аутентификации пользовательского аккаунта. Без него
    /// вход с включённым 2FA не завершится. Секрет.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Адрес MTProto-прокси: имя службы compose внутри сети, <c>localhost</c> при
    /// запуске на хосте.
    /// </summary>
    /// <remarks>
    /// Прямой доступ к Telegram с машины владельца закрыт, поэтому клиент идёт
    /// в прокси, а тот уже в Telegram. Пусто — прямой выход, как в бою Telegram
    /// доступен.
    /// </remarks>
    public string ProxyServer { get; set; } = string.Empty;

    /// <summary>Порт MTProto-прокси.</summary>
    public int ProxyPort { get; set; } = 1443;

    /// <summary>
    /// Код подтверждения входа, который Telegram присылает в приложение при первом
    /// входе нового устройства.
    /// </summary>
    /// <remarks>
    /// Нужен один раз: после того как сессия легла в БД, клиент входит сам. Пока
    /// сессии нет, вход без кода не состоится, и сервис об этом скажет в лог.
    /// Секрет.
    /// </remarks>
    public string LoginCode { get; set; } = string.Empty;

    /// <summary>
    /// Секрет прокси. Один и тот же, что и у контейнера прокси: без него
    /// прокси не примет подключение.
    /// </summary>
    public string ProxySecret { get; set; } = string.Empty;
}
