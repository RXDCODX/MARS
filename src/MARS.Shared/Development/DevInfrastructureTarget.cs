namespace MARS.Shared.Development;

/// <summary>
/// Адрес инфраструктуры, взятый из конфигурации сервиса.
/// </summary>
/// <param name="Service">Имя службы в <c>docker-compose.yml</c>: postgres, rabbitmq, tempo.</param>
/// <param name="Host">Хост, который сервис получил из своей конфигурации.</param>
/// <param name="Port">Порт того же хоста.</param>
/// <param name="Source">
/// Ключ конфигурации, откуда взят адрес. Попадает в лог: иначе непонятно, по
/// какой строке решено, что база не отвечает.
/// </param>
public sealed record DevInfrastructureTarget(string Service, string Host, int Port, string Source)
{
    /// <summary>Адрес для лога.</summary>
    public string Address => $"{Host}:{Port}";

    /// <summary>
    /// Локальный ли адрес. Автостарт осмыслен только для локальных: в контейнере
    /// хост — это имя службы compose (postgres, rabbitmq, tempo), и поднимать
    /// инфраструктуру изнутри контейнера нечем.
    /// </summary>
    public bool IsLocal => IsLocalHost(Host);

    /// <summary>Имена хоста, которые видит сам хост-процесс.</summary>
    /// <param name="host">Хост из конфигурации.</param>
    public static bool IsLocalHost(string? host)
    {
        var local =
            string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "127.0.0.1", StringComparison.Ordinal)
            || string.Equals(host, "::1", StringComparison.Ordinal)
            || string.Equals(host, "[::1]", StringComparison.Ordinal);

        return local;
    }
}
