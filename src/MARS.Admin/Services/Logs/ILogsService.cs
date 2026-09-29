using MARS.Admin.Entities;

namespace MARS.Admin.Services.Logs;

public interface ILogsService
{
    Task<(IEnumerable<Log> Logs, int TotalCount)> GetLogsAsync(
        int page = 1,
        int pageSize = 50,
        string? sortBy = null,
        bool sortDescending = true,
        LogLevel? logLevel = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? searchText = null
    );

    Task<IEnumerable<Log>> GetLogsByLevelAsync(LogLevel logLevel);
    Task<IEnumerable<Log>> GetLogsByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<IEnumerable<Log>> GetRecentLogsAsync(int count = 100);
    Task<LogsStatistics> GetLogsStatisticsAsync();
}

public class LogsStatistics
{
    public int TotalLogs { get; set; }
    public int WarningLogs { get; set; }
    public int ErrorLogs { get; set; }
    public int CriticalLogs { get; set; }
    public DateTime? OldestLogDate { get; set; }
    public DateTime? NewestLogDate { get; set; }
}
