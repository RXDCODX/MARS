using Microsoft.Extensions.Logging;

namespace MARS.Admin.CustomLoggers.DatabaseLogger;

[ProviderAlias("Database")]
public class DbLoggerProvider(DbLoggerOptions options) : ILoggerProvider
{
    public readonly DbLoggerOptions Options = options;

    public ILogger CreateLogger(string categoryName)
    {
        return new DbLogger(this);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
