using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MARS.Admin.CustomLoggers.SignalRLogger;

public static class SignalRLoggerExtensions
{
    public static ILoggingBuilder AddSignalRLogger(
        this ILoggingBuilder builder,
        Action<SignalRLoggerOptions>? configure = null
    )
    {
        var options = new SignalRLoggerOptions();
        configure?.Invoke(options);

        builder.Services.AddSingleton<LoggerHubRecursionGuard>();

        builder.Services.AddSingleton<ILoggerProvider>(serviceProvider => new SignalRLoggerProvider(
            options,
            null,
            serviceProvider.GetRequiredService<LoggerHubRecursionGuard>()
        ));

        return builder;
    }

    public static ILoggingBuilder AddSignalRLogger(
        this ILoggingBuilder builder,
        Func<string, LogLevel, bool> filter,
        Action<SignalRLoggerOptions>? configure = null
    )
    {
        var options = new SignalRLoggerOptions();
        configure?.Invoke(options);

        builder.Services.AddSingleton<LoggerHubRecursionGuard>();

        builder.Services.AddSingleton<ILoggerProvider>(serviceProvider => new SignalRLoggerProvider(
            options,
            filter,
            serviceProvider.GetRequiredService<LoggerHubRecursionGuard>()
        ));

        return builder;
    }
}
