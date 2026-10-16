using Microsoft.Extensions.Configuration;

namespace MARS.Shared.Development;

/// <summary>
/// Собирает адреса инфраструктуры из конфигурации сервиса.
/// </summary>
/// <remarks>
/// <para>
/// «Поднята ли инфраструктура» решается пробой того, что профиль MARS.Local и так
/// знает: своей строки подключения, адреса брокера и адреса OTLP. Отдельного
/// списка адресов нет намеренно — он был бы вторым источником правды рядом с
/// конфигурацией и разошёлся бы с ней молча.
/// </para>
/// <para>
/// Функция чистая: ни сокетов, ни файлов, поэтому проверяется юнит-тестами, а не
/// живым стендом.
/// </para>
/// </remarks>
public static class DevInfrastructureTargetResolver
{
    /// <summary>Имя службы postgres в compose.</summary>
    public const string PostgresService = "postgres";

    /// <summary>Имя службы брокера в compose.</summary>
    public const string RabbitMqService = "rabbitmq";

    /// <summary>Имя службы трейсов в compose.</summary>
    public const string TempoService = "tempo";

    /// <summary>Порт postgres по умолчанию: в строке подключения его может не быть.</summary>
    private const int DefaultPostgresPort = 5432;

    /// <summary>Порт брокера по умолчанию.</summary>
    private const int DefaultRabbitMqPort = 5672;

    /// <summary>Секция строк подключения.</summary>
    private const string ConnectionStringsSection = "ConnectionStrings";

    /// <summary>Ключ адреса брокера.</summary>
    private const string RabbitMqHostKey = "RabbitMq:Host";

    /// <summary>Ключ порта брокера.</summary>
    private const string RabbitMqPortKey = "RabbitMq:Port";

    /// <summary>Ключ адреса приёмника трейсов.</summary>
    private const string OtlpEndpointKey = "Otlp:Endpoint";

    /// <summary>
    /// Все адреса инфраструктуры, которые видит сервис. Разные базы одного хоста и
    /// порта схлопываются: пробовать один и тот же порт несколько раз незачем.
    /// </summary>
    /// <param name="configuration">Конфигурация сервиса, включая переменные окружения.</param>
    public static IReadOnlyList<DevInfrastructureTarget> Resolve(IConfiguration configuration)
    {
        var targets = new List<DevInfrastructureTarget>();

        AddPostgresTargets(configuration, targets);
        AddRabbitMqTarget(configuration, targets);
        AddTempoTarget(configuration, targets);

        return targets;
    }

    /// <summary>Свои базы сервиса: каждая строка подключения, где указан хост.</summary>
    private static void AddPostgresTargets(
        IConfiguration configuration,
        List<DevInfrastructureTarget> targets
    )
    {
        foreach (var connection in configuration.GetSection(ConnectionStringsSection).GetChildren())
        {
            if (TryReadPostgresAddress(connection.Value, out var host, out var port))
            {
                Add(
                    targets,
                    PostgresService,
                    host,
                    port,
                    $"{ConnectionStringsSection}:{connection.Key}"
                );
            }
        }
    }

    /// <summary>Брокер: адрес берётся из той же секции, что и у соединения.</summary>
    private static void AddRabbitMqTarget(
        IConfiguration configuration,
        List<DevInfrastructureTarget> targets
    )
    {
        var host = configuration[RabbitMqHostKey];
        var port = ReadPort(configuration[RabbitMqPortKey], DefaultRabbitMqPort);

        // Пустой Host означает, что брокер сервису не нужен: RabbitMqConsumerBase
        // регистрируется только при непустом адресе.
        if (!string.IsNullOrWhiteSpace(host))
        {
            Add(targets, RabbitMqService, host, port, RabbitMqHostKey);
        }
    }

    /// <summary>Трейсы: адрес OTLP-приёмника указывает на tempo.</summary>
    private static void AddTempoTarget(
        IConfiguration configuration,
        List<DevInfrastructureTarget> targets
    )
    {
        var endpoint = configuration[OtlpEndpointKey];

        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) && uri.Port > 0)
        {
            Add(targets, TempoService, uri.Host, uri.Port, OtlpEndpointKey);
        }
    }

    /// <summary>
    /// Разбор адреса postgres из строки подключения.
    /// </summary>
    /// <remarks>
    /// Разбор ручной, а не через <c>DbConnectionStringBuilder</c>: строку пишет
    /// человек, и падать на незнакомом параметре незачем — нужны только Host и
    /// Port, остальное пусть читает Npgsql.
    /// </remarks>
    private static bool TryReadPostgresAddress(
        string? connectionString,
        out string host,
        out int port
    )
    {
        host = string.Empty;
        port = DefaultPostgresPort;

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = part.IndexOf('=', StringComparison.Ordinal);

                if (separator > 0)
                {
                    var name = part[..separator].Trim();
                    var value = part[(separator + 1)..].Trim();

                    if (IsHostKey(name))
                    {
                        host = value;
                    }
                    else if (string.Equals(name, "Port", StringComparison.OrdinalIgnoreCase))
                    {
                        port = ReadPort(value, DefaultPostgresPort);
                    }
                }
            }
        }

        return !string.IsNullOrWhiteSpace(host);
    }

    /// <summary>Имена параметра строки подключения, задающего хост.</summary>
    private static bool IsHostKey(string name)
    {
        var isHostKey =
            string.Equals(name, "Host", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Server", StringComparison.OrdinalIgnoreCase);

        return isHostKey;
    }

    /// <summary>Порт из конфигурации: нечисловое значение не должно ронять старт.</summary>
    private static int ReadPort(string? text, int fallback)
    {
        var port = int.TryParse(text, out var parsed) ? parsed : fallback;

        return port;
    }

    /// <summary>Добавляет адрес, если адреса с теми же службой, хостом и портом ещё нет.</summary>
    private static void Add(
        List<DevInfrastructureTarget> targets,
        string service,
        string host,
        int port,
        string source
    )
    {
        var known = targets.Any(existing =>
            string.Equals(existing.Service, service, StringComparison.Ordinal)
            && string.Equals(existing.Host, host, StringComparison.OrdinalIgnoreCase)
            && existing.Port == port
        );

        if (!known)
        {
            targets.Add(new DevInfrastructureTarget(service, host, port, source));
        }
    }
}
