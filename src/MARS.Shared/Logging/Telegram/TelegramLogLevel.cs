using Serilog.Events;

namespace MARS.Shared.Logging.Telegram;

/// <summary>
/// Разбор порога уровня записи из конфигурации.
/// </summary>
/// <remarks>
/// Уровни — те же, что у Serilog, потому что сборщик видит именно его события.
/// У Serilog нет Critical, а у Microsoft Logging.Level он есть и выше Error:
/// Critical отображается в Fatal, иначе порог «Critical» оказался бы строже
/// «Error» и пропускал бы часть ошибок.
///
/// Неразобранное значение заменяется значением по умолчанию, а не остаётся
/// «без порога»: опечатка в конфигурации не должна молча отключить доставку.
/// </remarks>
public static class TelegramLogLevel
{
    /// <summary>Порог по умолчанию: ошибки и только они.</summary>
    public const LogEventLevel Default = LogEventLevel.Error;

    /// <summary>
    /// Разбирает имя уровня. Пустое и незнакомое значение даёт порог по
    /// умолчанию.
    /// </summary>
    public static LogEventLevel Parse(string? value)
    {
        var result = Default;

        if (!string.IsNullOrWhiteSpace(value))
        {
            var normalized = value.Trim();

            if (Is(normalized, "Verbose") || Is(normalized, "Trace"))
            {
                result = LogEventLevel.Verbose;
            }
            else if (Is(normalized, "Debug"))
            {
                result = LogEventLevel.Debug;
            }
            else if (Is(normalized, "Information") || Is(normalized, "Info"))
            {
                result = LogEventLevel.Information;
            }
            else if (Is(normalized, "Warning") || Is(normalized, "Warn"))
            {
                result = LogEventLevel.Warning;
            }
            else if (Is(normalized, "Error"))
            {
                result = LogEventLevel.Error;
            }
            else if (Is(normalized, "Critical") || Is(normalized, "Fatal"))
            {
                result = LogEventLevel.Fatal;
            }
        }

        return result;
    }

    private static bool Is(string value, string name) =>
        string.Equals(value, name, StringComparison.OrdinalIgnoreCase);
}
