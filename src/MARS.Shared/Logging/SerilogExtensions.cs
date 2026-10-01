using Microsoft.Extensions.Configuration;
using Serilog;

namespace MARS.Shared.Logging;

public static class SerilogExtensions
{
    /// <summary>
    /// Базовая настройка логирования сервиса: консоль и OTLP→Tempo.
    ///
    /// Логи в Loki доставляет Grafana Alloy — он читает stdout контейнеров через
    /// Docker-демон. Раньше здесь был синк Serilog.Sinks.Seq, но это давало
    /// второй, отдельный интерфейс логов рядом с Grafana и требовало отдельного
    /// UI, пароля и порта. Логи приложений теперь видны в Grafana наравне с
    /// метриками и трейсами.
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

        var otlpEndpoint = configuration["Otlp:Endpoint"];
        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            loggerConfig.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = otlpEndpoint;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = serviceName,
                };
            });
        }

        return loggerConfig;
    }
}
