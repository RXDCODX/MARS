using System.Text;
using MARS.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace MARS.Shared.Logging;

public static class SerilogExtensions
{
    /// <summary>
    /// Базовая настройка логирования сервиса: консоль и OTLP-логи в Alloy.
    ///
    /// Пути наблюдаемости разные и не должны сходиться: трейсы по-прежнему идут
    /// на <c>Otlp:Endpoint</c> в Tempo, а логи — на <c>Serilog:OtlpEndpoint</c> в
    /// Grafana Alloy, который раскладывает их в Loki. Tempo логи не хранит,
    /// поэтому отправка записей туда означала бы их потерю.
    ///
    /// Alloy превращает атрибуты записи в метки Loki по подсказке
    /// <c>loki.resource.labels</c>, а Grafana ищет по метке <c>container</c>.
    /// Поэтому в ресурс кладётся имя compose-сервиса в kebab-case
    /// (<c>twitch-core</c>), а не <c>MARS.TwitchCore</c>: значения в трейсах и
    /// логах для одного сервиса обязаны совпадать, иначе корреляция между ними
    /// молча не находит ничего.
    ///
    /// Файловый синк ниже включается только хост-запуску (<c>Dev:LogDirectory</c>):
    /// контейнеру он не нужен, потому что его stdout уже читает Alloy. Seq
    /// выпилен, единственный UI логов — Grafana.
    /// </summary>
    public static LoggerConfiguration AddMarsLogging(
        this LoggerConfiguration loggerConfig,
        IConfiguration configuration,
        string serviceName
    )
    {
        loggerConfig
            .Enrich.WithProperty("ServiceName", serviceName)
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.FromLogContext()
            .Enrich.With<ActivityEnricher>()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] {Message:lj}{NewLine}{Exception}"
            );

        var logEndpoint = ResolveLogEndpoint(configuration);
        if (!string.IsNullOrEmpty(logEndpoint))
        {
            loggerConfig.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = logEndpoint;
                // Приёмник otelcol.receiver.otlp в config.alloy открывает только
                // grpc-сокет (порт 4317), а синк по умолчанию шлёт HTTP/protobuf —
                // без явного протокола записи молча не доходят до Loki.
                options.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = serviceName,
                    ["container"] = ToContainerName(serviceName),
                };
            });
        }

        var logDirectory = configuration["Dev:LogDirectory"];
        if (!string.IsNullOrWhiteSpace(logDirectory))
        {
            var composeName = ComposeServiceNames.From(serviceName);
            if (composeName is not null)
            {
                try
                {
                    Directory.CreateDirectory(logDirectory);
                    loggerConfig.WriteTo.File(
                        Path.Combine(logDirectory, composeName + ".log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 3,
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] {Message:lj}{NewLine}{Exception}"
                    );
                }
                catch (Exception)
                {
                    // Файловый сток нужен только отладке; его отсутствие не должно
                    // ронять запуск сервиса.
                }
            }
        }

        return loggerConfig;
    }

    /// <summary>
    /// Эндпоинт OTLP-логов: <c>Serilog:OtlpEndpoint</c>, а при его отсутствии —
    /// прежний <c>Otlp:Endpoint</c>. Пустая строка считается незаданным
    /// значением: compose подставляет её для отсутствующей переменной, и она не
    /// должна перебивать запасной ключ.
    /// </summary>
    public static string? ResolveLogEndpoint(IConfiguration configuration)
    {
        var result = configuration["Serilog:OtlpEndpoint"];

        if (string.IsNullOrEmpty(result))
        {
            result = configuration["Otlp:Endpoint"];
        }

        return string.IsNullOrEmpty(result) ? null : result;
    }

    /// <summary>
    /// Имя compose-сервиса из имени сервиса: <c>MARS.MediaStorage</c> →
    /// <c>media-storage</c>. Совпадает с тем, что Alloy берёт из имени
    /// контейнера, поэтому метка <c>container</c> в Loki едина для строк stdout
    /// и для OTLP-записей.
    /// </summary>
    public static string ToContainerName(string serviceName)
    {
        var result = serviceName;

        if (result.StartsWith("MARS.", StringComparison.Ordinal))
        {
            result = result["MARS.".Length..];
        }

        var builder = new StringBuilder(result.Length + 4);
        for (var index = 0; index < result.Length; index++)
        {
            var current = result[index];
            if (char.IsUpper(current) && index > 0)
            {
                var previousIsLower = char.IsLower(result[index - 1]);
                var nextIsLower = index + 1 < result.Length && char.IsLower(result[index + 1]);
                if (previousIsLower || nextIsLower)
                {
                    builder.Append('-');
                }
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }
}
