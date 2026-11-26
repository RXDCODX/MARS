using Serilog.Core;
using Serilog.Events;

namespace MARS.Platform.Infrastructure.Observability.Logging;

/// <summary>
/// Добавляет уровень записи в свойства Serilog, чтобы Alloy мог превратить его в
/// метку Loki и дашборд фильтровал журналы без разбора текста.
/// </summary>
public sealed class LogLevelEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("level", logEvent.Level.ToString())
        );
    }
}
