using MARS.Admin.Hubs;
using MARS.Admin.Hubs.Interfaces;
using MARS.Admin.Hubs.Models.LoggerHub;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace MARS.Admin.CustomLoggers.SignalRLogger;

public class SignalRLogger(
    string category,
    SignalRLoggerOptions options,
    Func<string, LogLevel, bool>? filter,
    LoggerHubRecursionGuard recursionGuard
) : ILogger
{
    public static IHubContext<LoggerHub, ILoggerHub>? HubContext { get; set; }

    private readonly Func<string, LogLevel, bool> _filter = filter ?? ((cat, logLevel) => true);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (!IsEnabled(logLevel) || HubContext is null)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(formatter);

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception == null)
        {
            return;
        }

        if (recursionGuard.ShouldSkipLog(category, message))
        {
            return;
        }

        if (logLevel < options.MinimumLogLevel)
        {
            return;
        }

        if (
            options.ExcludedCategories?.Contains(category, StringComparer.OrdinalIgnoreCase) == true
        )
        {
            return;
        }

        if (
            options.IncludedCategories?.Count > 0
            && !options.IncludedCategories.Contains(category, StringComparer.OrdinalIgnoreCase)
        )
        {
            return;
        }

        var logMessage = new LogMessageDto
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.Now,
            LogLevel = logLevel.ToString(),
            Category = category,
            Message = message,
            Exception = exception?.Message,
            StackTrace = exception?.StackTrace,
            EventId = eventId.Id,
            Source = options.SourceName,
        };

        Task.Factory.StartNew(async () =>
        {
            try
            {
                var suppressionScope = recursionGuard.BeginSuppression();

                try
                {
                    await HubContext.Clients.All.Log(logMessage);
                }
                finally
                {
                    suppressionScope.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending log via SignalR: {ex.Message}");
            }
        });
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _filter(category, logLevel) && logLevel != LogLevel.None;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }
}
