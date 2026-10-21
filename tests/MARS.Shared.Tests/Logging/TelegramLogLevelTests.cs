using MARS.Shared.Logging.Telegram;
using Serilog.Events;

namespace MARS.Shared.Tests.Logging;

/// <summary>
/// Порог уровня записи разбирается из конфигурации.
///
/// У Serilog нет уровня Critical, а у Microsoft Logging.Level он есть и выше
/// Error: Critical обязан отображаться в Fatal, иначе порог «Critical» оказался
/// бы строже «Error» и пропускал бы часть ошибок.
/// </summary>
public class TelegramLogLevelTests
{
    [Theory]
    [InlineData("Verbose", LogEventLevel.Verbose)]
    [InlineData("verbose", LogEventLevel.Verbose)]
    [InlineData("Trace", LogEventLevel.Verbose)]
    [InlineData("Debug", LogEventLevel.Debug)]
    [InlineData("Information", LogEventLevel.Information)]
    [InlineData("Info", LogEventLevel.Information)]
    [InlineData("Warning", LogEventLevel.Warning)]
    [InlineData("Warn", LogEventLevel.Warning)]
    [InlineData("Error", LogEventLevel.Error)]
    [InlineData("Critical", LogEventLevel.Fatal)]
    [InlineData("Fatal", LogEventLevel.Fatal)]
    public void KnownLevelsAreParsed(string value, LogEventLevel expected)
    {
        Assert.Equal(expected, TelegramLogLevel.Parse(value));
    }

    /// <summary>
    /// Опечатка в конфигурации не должна отключать доставку: остаётся порог по
    /// умолчанию, а не «доставлять всё».
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ошибка")]
    [InlineData("ERR")]
    public void UnknownValuesFallBackToError(string? value)
    {
        Assert.Equal(LogEventLevel.Error, TelegramLogLevel.Parse(value));
    }

    [Fact]
    public void DefaultIsError()
    {
        Assert.Equal(LogEventLevel.Error, TelegramLogLevel.Default);
    }
}
