using System.Diagnostics.Metrics;
using Serilog.Core;
using Serilog.Events;

namespace MARS.Platform.Infrastructure.Observability.Logging;

/// <summary>
/// Счётчик записей журнала по службе и уровню. Нужен, чтобы Prometheus мог
/// строить алерты на любое появление Warning/Error без разбора текста логов:
/// счётчик попадает в /metrics через OpenTelemetryPrometheusBridge, как и
/// остальные счётчики MARS.*.
/// </summary>
public static class LogLevelMetrics
{
    private static readonly Meter Meter = new("MARS.Logging", "1.0.0");

    public static readonly Counter<long> LogEvents = Meter.CreateCounter<long>(
        "mars.log.events",
        description: "Log events by service and level",
        unit: null,
        tags:
        [
            new KeyValuePair<string, object?>("service", string.Empty),
            new KeyValuePair<string, object?>("level", string.Empty),
        ]
    );
}
