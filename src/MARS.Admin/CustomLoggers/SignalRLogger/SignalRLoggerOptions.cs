namespace MARS.Admin.CustomLoggers.SignalRLogger;

public class SignalRLoggerOptions
{
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;
    public string SourceName { get; set; } = "MARS.Admin";
    public HashSet<string>? ExcludedCategories { get; set; }
    public HashSet<string>? IncludedCategories { get; set; }
    public int MaxMessageLength { get; set; } = 1000;
    public bool IncludeExceptions { get; set; } = true;
    public bool IncludeStackTrace { get; set; } = true;
}
