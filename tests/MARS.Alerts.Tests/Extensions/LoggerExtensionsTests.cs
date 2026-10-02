using MARS.Alerts.Extensions;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Tests.Extensions;

/// <summary>
/// Запись исключений в лог.
///
/// Проверяется, что в лог уходит сообщение **корневой** причины, а не внешней
/// обёртки: в цепочке вида «поток не отправлен → таймаут → соединение отклонено» именно
/// последняя строка объясняет, что чинить. Собственное сообщение внешней ошибки
/// при этом тоже сохраняется — в общем виде.
/// </summary>
public class LoggerExtensionsTests
{
    [Fact]
    public void InnermostMessageIsLogged()
    {
        var logger = new RecordingLogger();

        logger.LogException(
            new InvalidOperationException(
                "внешняя ошибка",
                new TimeoutException("внутренняя ошибка")
            )
        );

        Assert.Contains("внутренняя ошибка", logger.LastMessage);
    }

    /// <summary>
    /// Стек-трейс попадает в лог: без него ошибка из фоновой задачи выглядит
    /// как голое сообщение без указания, где именно она возникла.
    /// </summary>
    [Fact]
    public void StackTraceIsLogged()
    {
        var logger = new RecordingLogger();

        logger.LogException(Thrown());

        Assert.Contains(nameof(LoggerExtensionsTests), logger.LastMessage);
    }

    /// <summary>
    /// Исключение обязано быть брошено: у неброшенного стек-трейса нет, и
    /// проверять было бы нечего.
    /// </summary>
    private static InvalidOperationException Thrown()
    {
        try
        {
            throw new InvalidOperationException("ошибка");
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }
    }

    [Fact]
    public void WholeChainIsLoggedForTypedLogger()
    {
        var logger = new RecordingLogger<SomeCategory>();

        logger.LogException(
            new InvalidOperationException(
                "первая",
                new TimeoutException("вторая", new IOException("третья"))
            )
        );

        // Имя категории в сообщении нужно, чтобы в общем логе понять, откуда
        // пришло исключение среди десятков сервисов.
        Assert.Contains(nameof(SomeCategory), logger.LastMessage);
        Assert.Contains("вторая", logger.LastMessage);
        Assert.Contains("третья", logger.LastMessage);
    }

    [Fact]
    public void SingleExceptionIsLoggedAsIs()
    {
        var logger = new RecordingLogger<SomeCategory>();

        logger.LogException(new InvalidOperationException("единственная"));

        Assert.Contains("единственная", logger.LastMessage);
    }

    private sealed class SomeCategory;

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public string LastMessage { get; private set; } = string.Empty;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            LastMessage = formatter(state, exception) ?? string.Empty;
        }
    }

    /// <summary>
    /// Обычный логгер пишет шаблон с подстановками через <c>Formatter</c>, поэтому
    /// проверяется собранный текст, а не отдельные аргументы.
    /// </summary>
    private sealed class RecordingLogger : ILogger
    {
        public string LastMessage { get; private set; } = string.Empty;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            LastMessage = formatter(state, exception) ?? string.Empty;
        }
    }
}
