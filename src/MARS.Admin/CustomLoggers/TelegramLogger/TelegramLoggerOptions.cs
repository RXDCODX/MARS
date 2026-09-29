using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace MARS.Admin.CustomLoggers.TelegramLogger;

public class TelegramLoggerOptions
{
#pragma warning disable CS8618
    public string BotToken { get; set; }
    public long[] ChatId { get; set; }
    public string SourceName { get; set; }
#pragma warning restore CS8618
    public LogLevel MinimumLevel { get; set; } = LogLevel.None;
}
