using System.Diagnostics.Metrics;
using MARS.Platform.Infrastructure.Observability.Logging;
using Serilog.Events;

namespace MARS.Shared.Tests.Logging;

/// <summary>
/// Счётчик уровней журнала: каждая запись инкрементирует счётчик с метками
/// service и level. Без этого Prometheus не сможет построить алерт на любое
/// появление Warning/Error.
/// </summary>
public class LogLevelMetricsSinkTests
{
    [Fact]
    public void Emit_Увеличивает_Счётчик_ПоУровню_И_Сервису()
    {
        var measurements = new Dictionary<(string Service, string Level), long>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == "MARS.Logging" && instrument.Name == "mars.log.events")
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            var service = tags.ToArray().FirstOrDefault(t => t.Key == "service").Value?.ToString() ?? string.Empty;
            var level = tags.ToArray().FirstOrDefault(t => t.Key == "level").Value?.ToString() ?? string.Empty;
            var key = (service, level);
            measurements[key] = measurements.GetValueOrDefault(key) + value;
        });
        listener.Start();

        var sink = new LogLevelMetricsSink("test-service");
        var parser = new Serilog.Parsing.MessageTemplateParser();
        var template = parser.Parse("msg");
        sink.Emit(new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Warning, null, template, []));
        sink.Emit(new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, template, []));
        sink.Emit(new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, template, []));

        Assert.Equal(1, measurements.GetValueOrDefault(("test-service", "Warning")));
        Assert.Equal(2, measurements.GetValueOrDefault(("test-service", "Error")));
    }
}