using System.Diagnostics.Metrics;
using Serilog.Core;
using Serilog.Events;

namespace MARS.Platform.Infrastructure.Observability.Logging;

/// <summary>
/// Синк Serilog, считающий каждую запись в счётчике <see cref="LogLevelMetrics"/>.
/// </summary>
/// <remarks>
/// Живёт в каждой службе и включён по умолчанию (регистрируется в
/// <see cref="SerilogExtensions.AddMarsLogging"/>). Пороги и агрегация
/// выносятся в правила Prometheus: здесь только факт появления записи.
/// </remarks>
public sealed class LogLevelMetricsSink : ILogEventSink
{
    private readonly string _serviceName;

    public LogLevelMetricsSink(string serviceName)
    {
        _serviceName = serviceName;
    }

    public void Emit(LogEvent logEvent)
    {
        LogLevelMetrics.LogEvents.Add(
            1,
            new KeyValuePair<string, object?>("service", _serviceName),
            new KeyValuePair<string, object?>("level", logEvent.Level.ToString())
        );
    }
}
