using System.Diagnostics;
using System.Runtime.InteropServices;
using MARS.Shared.Models;
using MARS.TwitchCore.DTOs;
using MARS.TwitchCore.Services.Connection;
using MARS.TwitchCore.Services.EventSub;
using MARS.TwitchCore.Services.PuntoSwitcher;
using MARS.TwitchCore.Services.WeddingAnniversary;
using Microsoft.AspNetCore.Mvc;

namespace MARS.TwitchCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServerStatsController(
    ILogger<ServerStatsController> logger,
    EventSubService eventSubService,
    ITwitchConnectionState twitchConnectionManager,
    WeddingAnniversaryService weddingAnniversaryService,
    IPuntoSwitcherService puntoSwitcherService
) : ControllerBase
{
    private static readonly Stopwatch UptimeStopwatch = Stopwatch.StartNew();

    [HttpGet]
    public async Task<ActionResult<OperationResult<ServerStatsResponse>>> GetStats(
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<ServerStatsResponse>> result;
        try
        {
            var process = Process.GetCurrentProcess();
            var totalMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            var workingSet = process.WorkingSet64;
            var privateMemory = process.PrivateMemorySize64;
            var gcHeap = GC.GetTotalMemory(forceFullCollection: false);
            var cpuUsage = GetCpuUsage(process);

            var nearestAnniversary = await weddingAnniversaryService.GetNearestAnniversaryAsync(
                cancellationToken
            );

            var stats = new ServerStatsResponse
            {
                CpuUsagePercent = cpuUsage,
                MemoryWorkingSetBytes = workingSet,
                MemoryPrivateBytes = privateMemory,
                MemoryGcHeapBytes = gcHeap,
                MemoryTotalBytes = totalMemory,
                UptimeSeconds = UptimeStopwatch.Elapsed.TotalSeconds,
                ThreadCount = process.Threads.Count,
                OsVersion = Environment.OSVersion.ToString(),
                RuntimeVersion = RuntimeInformation.FrameworkDescription,
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                IsEventSubConnected = eventSubService.IsWebSocketConnected,
                IsTwitchChatConnected = twitchConnectionManager.IsConnected,
                IsPuntoSwitcherEnabled = puntoSwitcherService.IsFilterEnabled,
                NearestWeddingAnniversaryName = nearestAnniversary?.AnniversaryName,
                NearestWeddingAnniversaryDate = nearestAnniversary?.AnniversaryDate,
                NearestWeddingAnniversaryUser = nearestAnniversary?.DisplayName,
            };

            result = Ok(OperationResult<ServerStatsResponse>.Ok(stats));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении статистики сервера");
            result = Ok(
                OperationResult<ServerStatsResponse>.Fail("Ошибка при получении статистики сервера")
            );
        }

        return result;
    }

    private static double GetCpuUsage(Process process)
    {
        try
        {
            var cpuTime = process.TotalProcessorTime;
            var uptime =
                process.ExitTime == DateTime.MaxValue
                    ? DateTime.Now - process.StartTime.ToUniversalTime()
                    : process.ExitTime - process.StartTime;

            if (uptime.TotalMilliseconds <= 0)
            {
                return 0;
            }

            return Math.Round(
                (
                    cpuTime.TotalMilliseconds
                    / (uptime.TotalMilliseconds * Environment.ProcessorCount)
                ) * 100,
                2
            );
        }
        catch
        {
            return 0;
        }
    }
}
