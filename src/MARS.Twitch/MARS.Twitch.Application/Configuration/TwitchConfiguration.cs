namespace MARS.Twitch.Application.Configuration;

public class TwitchConfiguration
{
    /// <summary>
    /// Имя секции конфигурации.
    /// </summary>
    /// <remarks>
    /// Именно <c>Twitch</c>: с таким именем секцию задают переменные окружения
    /// файла стенда и читает <c>Program.cs</c> (<c>Twitch:ClientId</c>). Токены
    /// (OAuth чата, AccessToken/RefreshToken) в конфигурации больше не хранятся:
    /// они получаются только через OAuth-флоу самого сервиса.
    /// </remarks>
    public const string SectionName = "Twitch";

#pragma warning disable CS8618
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }

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
