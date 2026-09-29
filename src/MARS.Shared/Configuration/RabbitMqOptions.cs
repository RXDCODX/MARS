namespace MARS.Shared.Configuration;

/// <summary>
/// Настройки подключения к RabbitMQ.
/// Пароль может прийти двумя способами:
/// напрямую (<c>Password</c>) или путём к docker-secret (<c>Password_FILE</c>) —
/// второй вариант используется в docker-compose.
/// </summary>
public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "mars";
    public string Password { get; set; } = "mars";
    public string? PasswordFile { get; set; }
    public string VirtualHost { get; set; } = "/";

    /// <summary>Задержка перед повторной попыткой подключения/потребления, мс.</summary>
    public int ReconnectDelayMilliseconds { get; set; } = 5000;

    /// <summary>Сколько раз сообщение возвращается в очередь прежде чем уйти в dead-letter.</summary>
    public int MaxDeliveryAttempts { get; set; } = 3;
}
