using System.Diagnostics.CodeAnalysis;
using MARS.Admin.Entities;
using Microsoft.Extensions.Logging;

namespace MARS.Admin.CustomLoggers.DatabaseLogger;

public class DbLogger([NotNull] DbLoggerProvider dbLoggerProvider) : ILogger
{
    private readonly DbLoggerProvider _dbLoggerProvider = dbLoggerProvider;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public async void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (
            !IsEnabled(logLevel)
            || logLevel < _dbLoggerProvider.Options.MinimumLogLevel
            || !_dbLoggerProvider.Options.Environment.IsProduction()
        )
        {
            return;
        }

        await Task.Factory.StartNew(async () =>
        {
            try
            {
                await using var dbContext = _dbLoggerProvider.Options.Factory.CreateDbContext();

                var log = new Log
                {
                    Message = formatter(state, exception),
                    StackTrace = exception?.StackTrace,
                    LogLevel = logLevel,
                };

                await dbContext.Logs.AddAsync(log);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write log to database: {ex.Message}");
            }
        });
    }
}
