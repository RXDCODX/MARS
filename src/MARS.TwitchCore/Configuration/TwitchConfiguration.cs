namespace MARS.TwitchCore.Configuration;

public class TwitchConfiguration
{
    /// <summary>
    /// Имя секции конфигурации.
    /// </summary>
    /// <remarks>
    /// Именно <c>Twitch</c>: с таким именем секцию задают переменные окружения
    /// файла стенда (<c>Twitch__OAuth</c>, <c>Twitch__AccessToken</c>) и читает
    /// <c>Program.cs</c> (<c>Twitch:ClientId</c>). Имя <c>TwitchConfig</c> осталось
    /// после переноса из монолита, и из-за него OAuth чата не доходил до
    /// <c>TwitchConnectionManager</c>: токен читался пустым, а сервис пропускал
    /// подключение с предупреждением при заданной переменной окружения.
    /// </remarks>
    public const string SectionName = "Twitch";

#pragma warning disable CS8618
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }

    /// <summary>
    /// Токен для обращений к Twitch API. Служит и источником первого токена, если
    /// таблица <c>TwitchToken</c> пуста (см. <c>TokenService</c>).
    /// </summary>
    public string AccessToken { get; set; }

    /// <summary>
    /// Refresh-токен к <see cref="AccessToken"/>. Без него просроченный токен
    /// обновить нечем, и авторизацию придётся получать заново.
    /// </summary>
    public string RefreshToken { get; set; }

    /// <summary>OAuth-токен бота: подключение к чату.</summary>
    public string OAuth { get; set; }

    /// <summary>
    /// Срок действия <see cref="AccessToken"/>, если он известен.
    /// </summary>
    /// <remarks>
    /// Не задан — токен считается бессрочным, а не протухшим: нулевой срок
    /// читался бы как «истёк» и заставлял обновлять токен при каждой проверке.
    /// Токен, выданный Twitch на авторизацию пользователя, обычно срока не имеет
    /// вовсе, так что это обычное состояние, а не ошибка настройки.
    /// </remarks>
    public TimeSpan? AccessTokenExpiresIn { get; set; }

    /// <summary>
    /// Полный адрес обработчика кода авторизации — тот, что подставляется в
    /// ссылку авторизации и в обмен кода на токен.
    /// </summary>
    /// <remarks>
    /// Twitch требует, чтобы адрес совпадал в обоих местах, а внутри контейнера
    /// адрес самого сервиса браузеру недоступен: снаружи сервис публикуется через
    /// Gateway. Не задан — берётся адрес сервиса, чего хватает для запуска на хосте.
    /// </remarks>
    public string AuthRedirectUri { get; set; }

    /// <summary>
    /// Как часто повторяется просьба пройти авторизацию, пока токена нет.
    /// </summary>
    /// <remarks>
    /// Не задано — сутки. Меньше значит заметнее в чате, но чаще спам: цикл старта
    /// повторяет попытку каждые 30 секунд, и просьба без интервала уходила бы
    /// постоянно.
    /// </remarks>
    public TimeSpan AuthRequestInterval { get; set; } = TimeSpan.FromDays(1);
#pragma warning restore CS8618

    public string TransportProtocol { get; set; } = "WebSocket";
}
