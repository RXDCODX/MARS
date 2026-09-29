using MARS.Admin.Entities;
using MARS.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using MARS.Admin.Services;
using MARS.Admin.Services.Logs;
using Microsoft.AspNetCore.Mvc;

namespace MARS.Admin.Controllers;

[ApiController]
[Authorize(Policy = ServiceAuthExtensions.PolicyName)]
[Route("api/[controller]")]
public class LogsController(ILogsService logsService, ILogger<LogsController> logger)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(OperationResult<LogResponse>), 200)]
    public async Task<ActionResult<OperationResult<LogResponse>>> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = "whenlogged",
        [FromQuery] bool sortDescending = true,
        [FromQuery] LogLevel? logLevel = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? searchText = null
    )
    {
        ActionResult<OperationResult<LogResponse>> result;
        try
        {
            if (page < 1)
            {
                result = Ok(
                    OperationResult<LogResponse>.Bad(
                        "Номер страницы должен быть больше 0",
                        new LogResponse()
                    )
                );
            }
            else if (pageSize is < 1 or > 1000)
            {
                result = Ok(
                    OperationResult<LogResponse>.Bad(
                        "Размер страницы должен быть от 1 до 1000",
                        new LogResponse()
                    )
                );
            }
            else
            {
                var (logs, totalCount) = await logsService.GetLogsAsync(
                    page,
                    pageSize,
                    sortBy,
                    sortDescending,
                    logLevel,
                    fromDate,
                    toDate,
                    searchText
                );

                var response = new LogResponse
                {
                    Logs = logs,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                };

                result = Ok(OperationResult<LogResponse>.Ok("Логи успешно получены", response));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении логов");
            result = Ok(
                OperationResult<LogResponse>.Bad("Ошибка при получении логов", new LogResponse())
            );
        }

        return result;
    }

    [HttpGet("by-level/{logLevel}")]
    [ProducesResponseType(typeof(OperationResult<IEnumerable<Log>>), 200)]
    public async Task<ActionResult<OperationResult<IEnumerable<Log>>>> GetLogsByLevel(
        LogLevel logLevel
    )
    {
        ActionResult<OperationResult<IEnumerable<Log>>> result;
        try
        {
            var logs = await logsService.GetLogsByLevelAsync(logLevel);
            result = Ok(OperationResult<IEnumerable<Log>>.Ok("Логи получены по уровню", logs));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении логов по уровню {LogLevel}", logLevel);
            result = Ok(OperationResult<IEnumerable<Log>>.Bad("Ошибка при получении логов", []));
        }

        return result;
    }

    [HttpGet("by-date-range")]
    [ProducesResponseType(typeof(OperationResult<IEnumerable<Log>>), 200)]
    public async Task<ActionResult<OperationResult<IEnumerable<Log>>>> GetLogsByDateRange(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate
    )
    {
        ActionResult<OperationResult<IEnumerable<Log>>> result;
        try
        {
            if (fromDate > toDate)
            {
                result = Ok(
                    OperationResult<IEnumerable<Log>>.Bad(
                        "Дата начала должна быть меньше или равна дате окончания",
                        []
                    )
                );
            }
            else
            {
                var logs = await logsService.GetLogsByDateRangeAsync(fromDate, toDate);
                result = Ok(OperationResult<IEnumerable<Log>>.Ok("Логи получены за период", logs));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ошибка при получении логов за период с {FromDate} по {ToDate}",
                fromDate,
                toDate
            );
            result = Ok(OperationResult<IEnumerable<Log>>.Bad("Ошибка при получении логов", []));
        }

        return result;
    }

    [HttpGet("recent")]
    [ProducesResponseType(typeof(OperationResult<IEnumerable<Log>>), 200)]
    public async Task<ActionResult<OperationResult<IEnumerable<Log>>>> GetRecentLogs(
        [FromQuery] int count = 100
    )
    {
        ActionResult<OperationResult<IEnumerable<Log>>> result;
        try
        {
            if (count < 1 || count > 1000)
            {
                result = Ok(
                    OperationResult<IEnumerable<Log>>.Bad(
                        "Количество логов должно быть от 1 до 1000",
                        []
                    )
                );
            }
            else
            {
                var logs = await logsService.GetRecentLogsAsync(count);
                result = Ok(OperationResult<IEnumerable<Log>>.Ok("Получены последние логи", logs));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении последних {Count} логов", count);
            result = Ok(
                OperationResult<IEnumerable<Log>>.Bad("Ошибка при получении последних логов", [])
            );
        }

        return result;
    }

    [HttpGet("statistics")]
    [ProducesResponseType(typeof(OperationResult<LogsStatistics>), 200)]
    public async Task<ActionResult<OperationResult<LogsStatistics?>>> GetLogsStatistics()
    {
        ActionResult<OperationResult<LogsStatistics?>> result;
        try
        {
            var statistics = await logsService.GetLogsStatisticsAsync();
            result = Ok(
                OperationResult<LogsStatistics?>.Ok("Получена статистика логов", statistics)
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении статистики логов");
            result = Ok(
                OperationResult<LogsStatistics?>.Bad("Ошибка при получении статистики логов", null)
            );
        }

        return result;
    }

    [HttpPost("test")]
    public ActionResult<OperationResult<object>> CreateTestLog()
    {
        ActionResult<OperationResult<object>> result;
        try
        {
            logger.LogTrace("Тестовый лог уровня Trace");
            logger.LogDebug("Тестовый лог уровня Debug");
            logger.LogInformation("Тестовый лог уровня Information");
            logger.LogWarning("Тестовый лог уровня Warning");
            logger.LogError("Тестовый лог уровня Error");
            logger.LogCritical("Тестовый лог уровня Critical");

            var data = new { message = "Тестовые логи созданы", timestamp = DateTime.Now };
            result = Ok(OperationResult<object>.Ok("Тестовые логи успешно созданы", data));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании тестовых логов");
            result = Ok(OperationResult<object>.Bad("Ошибка при создании тестовых логов", new { }));
        }

        return result;
    }
}

public class LogResponse
{
    public IEnumerable<Log> Logs { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
