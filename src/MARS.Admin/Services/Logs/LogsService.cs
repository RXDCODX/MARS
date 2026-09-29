using MARS.Admin.Data;
using MARS.Admin.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.Admin.Services.Logs;

public class LogsService(LoggerDbContext dbContext) : ILogsService
{
    public async Task<(IEnumerable<Log> Logs, int TotalCount)> GetLogsAsync(
        int page = 1,
        int pageSize = 50,
        string? sortBy = null,
        bool sortDescending = true,
        LogLevel? logLevel = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? searchText = null
    )
    {
        (IEnumerable<Log> Logs, int TotalCount) result = ([], 0);

        if (page > 0 && pageSize > 0)
        {
            var query = dbContext.Logs.AsNoTracking();

            if (logLevel != null)
            {
                query = query.Where(l => l.LogLevel == logLevel);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(l => l.WhenLogged >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(l => l.WhenLogged <= toDate.Value);
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(l =>
                    l.Message.Contains(searchText)
                    || (l.StackTrace != null && l.StackTrace.Contains(searchText))
                );
            }

            var totalCount = await query.CountAsync();

            query = sortBy?.ToLower() switch
            {
                "whenlogged" => sortDescending
                    ? query.OrderByDescending(l => l.WhenLogged)
                    : query.OrderBy(l => l.WhenLogged),
                "loglevel" => sortDescending
                    ? query.OrderByDescending(l => l.LogLevel)
                    : query.OrderBy(l => l.LogLevel),
                "message" => sortDescending
                    ? query.OrderByDescending(l => l.Message)
                    : query.OrderBy(l => l.Message),
                _ => query.OrderByDescending(l => l.WhenLogged),
            };

            var logs = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            result = (logs, totalCount);
        }

        return result;
    }

    public async Task<IEnumerable<Log>> GetLogsByLevelAsync(LogLevel logLevel)
    {
        IEnumerable<Log> result = [];

        result = await dbContext
            .Logs.AsNoTracking()
            .Where(l => l.LogLevel == logLevel)
            .OrderByDescending(l => l.WhenLogged)
            .ToListAsync();

        return result;
    }

    public async Task<IEnumerable<Log>> GetLogsByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        IEnumerable<Log> result = [];

        if (fromDate <= toDate)
        {
            result = await dbContext
                .Logs.AsNoTracking()
                .Where(l => l.WhenLogged >= fromDate && l.WhenLogged <= toDate)
                .OrderByDescending(l => l.WhenLogged)
                .ToListAsync();
        }

        return result;
    }

    public async Task<IEnumerable<Log>> GetRecentLogsAsync(int count = 100)
    {
        IEnumerable<Log> result = [];

        if (count > 0)
        {
            result = await dbContext
                .Logs.AsNoTracking()
                .OrderByDescending(l => l.WhenLogged)
                .Take(count)
                .ToListAsync();
        }

        return result;
    }

    public async Task<LogsStatistics> GetLogsStatisticsAsync()
    {
        LogsStatistics result = new() { TotalLogs = 0 };

        result.TotalLogs = await dbContext.Logs.CountAsync();

        var levelStats = await dbContext
            .Logs.AsNoTracking()
            .GroupBy(l => l.LogLevel)
            .Select(g => new { LogLevel = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var stat in levelStats)
        {
            switch (stat.LogLevel)
            {
                case LogLevel.Warning:
                    result.WarningLogs = stat.Count;
                    break;
                case LogLevel.Error:
                    result.ErrorLogs = stat.Count;
                    break;
                case LogLevel.Critical:
                    result.CriticalLogs = stat.Count;
                    break;
            }
        }

        var oldestLog = await dbContext
            .Logs.AsNoTracking()
            .OrderBy(l => l.WhenLogged)
            .Select(l => l.WhenLogged)
            .FirstOrDefaultAsync();

        var newestLog = await dbContext
            .Logs.AsNoTracking()
            .OrderByDescending(l => l.WhenLogged)
            .Select(l => l.WhenLogged)
            .FirstOrDefaultAsync();

        result.OldestLogDate = oldestLog != default ? oldestLog : null;
        result.NewestLogDate = newestLog != default ? newestLog : null;

        return result;
    }
}
