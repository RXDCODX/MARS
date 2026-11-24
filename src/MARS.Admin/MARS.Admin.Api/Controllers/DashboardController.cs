using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using MARS.Admin.Api.Contracts;
using MARS.Admin.Application.Configuration;
using MARS.Admin.Application.Services.ServiceManager;
using MARS.Admin.Domain.Entities;
using MARS.Admin.Infrastructure.Postgres.Data;
using MARS.Platform.Infrastructure.Web.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MARS.Admin.Api.Controllers;

[ApiController]
[Authorize(Policy = ServiceAuthExtensions.PolicyName)]
[Route("api/[controller]")]
public class DashboardController(
    IServiceManager serviceManager,
    IHttpClientFactory httpClientFactory,
    IDbContextFactory<AdminDbContext> dbContextFactory,
    IConfiguration configuration,
    ILogger<DashboardController> logger
) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<OperationResult<DashboardOverviewResponse>>> GetOverview(
        CancellationToken cancellationToken = default
    )
    {
        var response = new DashboardOverviewResponse();

        try
        {
            // Per-service health + metrics.
            var services = (await serviceManager.GetAllServicesAsync()).ToList();
            var metrics = await FetchPrometheusMetricsAsync(cancellationToken);

            foreach (var svc in services)
            {
                var entry = new DashboardServiceEntry
                {
                    Name = svc.Name,
                    DisplayName = svc.DisplayName,
                    Status = svc.Status.ToString(),
                };

                if (metrics.TryGetValue(svc.Name, out var m))
                {
                    entry.CpuPercent = m.CpuPercent;
                    entry.MemoryBytes = m.MemoryBytes;
                    entry.GcHeapBytes = m.GcHeapBytes;
                    entry.UptimeSeconds = m.UptimeSeconds;
                    response.MetricsAvailable = true;
                }

                response.Services.Add(entry);
            }

            // Infrastructure probes.
            response.Infrastructure.Add(
                new DashboardInfraEntry
                {
                    Name = "postgres",
                    Status = await ProbePostgresAsync(cancellationToken) ? "online" : "offline",
                }
            );
            response.Infrastructure.Add(
                new DashboardInfraEntry
                {
                    Name = "rabbitmq",
                    Status = await ProbeHttpAsync(
                        "http://rabbitmq:15672/api/overview",
                        cancellationToken
                    )
                        ? "online"
                        : "offline",
                }
            );
            response.Infrastructure.Add(
                new DashboardInfraEntry
                {
                    Name = "matoi",
                    Status = await ProbeHttpAsync("http://matoi:3000/ping", cancellationToken)
                        ? "online"
                        : "offline",
                }
            );
            response.Infrastructure.Add(
                new DashboardInfraEntry
                {
                    Name = "tg-ws-proxy",
                    Status = await ProbeTcpAsync("tg-ws-proxy", 1443, cancellationToken)
                        ? "online"
                        : "offline",
                }
            );

            // Active alerts count.
            response.ActiveAlerts = await FetchActiveAlertsCountAsync(cancellationToken);

            // Connections: proxy twitch-core ServerStats.
            await PopulateConnectionsAsync(response, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to build dashboard overview");
        }

        return Ok(OperationResult<DashboardOverviewResponse>.Ok("Обзор получен", response));
    }

    [HttpGet("events")]
    public async Task<ActionResult<OperationResult<List<DashboardEventDto>>>> GetEvents(
        [FromQuery] string? type,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var query = db.EventLogs.AsNoTracking().OrderByDescending(e => e.OccurredAtUtc);
            if (!string.IsNullOrWhiteSpace(type))
            {
                query = (IOrderedQueryable<EventLog>)query.Where(e => e.Type == type);
            }

            var items = await query.Take(Math.Clamp(limit, 1, 500)).ToListAsync(cancellationToken);
            var dto = items
                .Select(e => new DashboardEventDto
                {
                    Id = e.Id,
                    Type = e.Type,
                    SourceService = e.SourceService,
                    OccurredAtUtc = e.OccurredAtUtc,
                    ActorDisplayName = e.ActorDisplayName,
                    PayloadJson = e.PayloadJson,
                })
                .ToList();

            return Ok(OperationResult<List<DashboardEventDto>>.Ok("События получены", dto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load dashboard events");
            return Ok(OperationResult<List<DashboardEventDto>>.Bad("Ошибка загрузки событий", []));
        }
    }

    [HttpPost("events")]
    public async Task<ActionResult<OperationResult>> PushEvent(
        [FromBody] DashboardEventDto dto,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var entry = new EventLog
            {
                Type = dto.Type,
                SourceService = dto.SourceService,
                OccurredAtUtc = dto.OccurredAtUtc == default ? DateTime.UtcNow : dto.OccurredAtUtc,
                ActorDisplayName = dto.ActorDisplayName,
                PayloadJson = string.IsNullOrWhiteSpace(dto.PayloadJson) ? "{}" : dto.PayloadJson,
            };
            await db.EventLogs.AddAsync(entry, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            // Broadcast to live subscribers.
            // (hub context not injected here; clients polling /events will see it)
            return Ok(OperationResult.Ok("Событие сохранено"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to push dashboard event");
            return Ok(OperationResult.Bad("Ошибка сохранения события"));
        }
    }

    [HttpGet("event-stats")]
    public async Task<ActionResult<OperationResult<DashboardEventStatsResponse>>> GetEventStats(
        [FromQuery] int hours = 24,
        CancellationToken cancellationToken = default
    )
    {
        var stats = new DashboardEventStatsResponse();
        try
        {
            var since = DateTime.UtcNow.AddHours(-Math.Clamp(hours, 1, 168));
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var items = await db
                .EventLogs.AsNoTracking()
                .Where(e => e.OccurredAtUtc >= since)
                .ToListAsync(cancellationToken);

            stats.Total = items.Count;
            foreach (var g in items.GroupBy(e => e.Type))
            {
                stats.ByType[g.Key] = g.Count();
            }

            stats.TopActors = items
                .Where(e => !string.IsNullOrWhiteSpace(e.ActorDisplayName))
                .GroupBy(e => e.ActorDisplayName!)
                .Select(g => (g.Key, g.Count()))
                .OrderByDescending(x => x.Item2)
                .Take(5)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to compute event stats");
        }

        return Ok(OperationResult<DashboardEventStatsResponse>.Ok("Статистика получена", stats));
    }

    private async Task<
        Dictionary<
            string,
            (double? CpuPercent, long? MemoryBytes, long? GcHeapBytes, double? UptimeSeconds)
        >
    > FetchPrometheusMetricsAsync(CancellationToken ct)
    {
        var result = new Dictionary<string, (double?, long?, long?, double?)>(
            StringComparer.OrdinalIgnoreCase
        );

        try
        {
            var baseUrl = configuration["Prometheus:BaseUrl"] ?? "http://prometheus:9090";
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var queries = new Dictionary<string, string>
            {
                ["mem"] = "process_working_set_bytes{job=\"mars-services\"}",
                ["cpu"] = "rate(process_cpu_seconds_total{job=\"mars-services\"}[1m])",
                ["gc"] = "dotnet_gc_heap_size_bytes{job=\"mars-services\"}",
                ["uptime"] = "process_start_time_seconds{job=\"mars-services\"}",
            };

            var responses = new Dictionary<string, JsonDocument?>();
            foreach (var (key, q) in queries)
            {
                try
                {
                    var url = $"{baseUrl}/api/v1/query?query={Uri.EscapeDataString(q)}";
                    using var resp = await client.GetAsync(url, ct);
                    if (resp.IsSuccessStatusCode)
                    {
                        responses[key] = await JsonDocument.ParseAsync(
                            await resp.Content.ReadAsStreamAsync(ct),
                            cancellationToken: ct
                        );
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Prometheus query {Key} failed", key);
                }
            }

            // Parse each vector result into per-instance values.
            var memByInstance = ParseVector(responses.GetValueOrDefault("mem"));
            var cpuByInstance = ParseVector(responses.GetValueOrDefault("cpu"));
            var gcByInstance = ParseVector(responses.GetValueOrDefault("gc"));
            var upByInstance = ParseVector(responses.GetValueOrDefault("uptime"));

            var instances = memByInstance
                .Keys.Union(cpuByInstance.Keys)
                .Union(gcByInstance.Keys)
                .Union(upByInstance.Keys);

            foreach (var inst in instances)
            {
                double? cpu = cpuByInstance.TryGetValue(inst, out var c) ? c * 100 : null;
                long? mem = memByInstance.TryGetValue(inst, out var m) ? (long)m : null;
                long? gc = gcByInstance.TryGetValue(inst, out var g) ? (long)g : null;
                double? uptime = null;
                if (upByInstance.TryGetValue(inst, out var u))
                {
                    uptime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - u;
                }

                result[inst] = (cpu, mem, gc, uptime);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Prometheus metrics fetch failed");
        }

        return result;
    }

    private static Dictionary<string, double> ParseVector(JsonDocument? doc)
    {
        var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (doc is null)
        {
            return result;
        }

        try
        {
            var data = doc.RootElement.GetProperty("data");
            var resultList = data.GetProperty("result");
            foreach (var item in resultList.EnumerateArray())
            {
                var instance = "unknown";
                if (
                    item.TryGetProperty("metric", out var metric)
                    && metric.TryGetProperty("instance", out var inst)
                )
                {
                    instance = inst.GetString() ?? "unknown";
                }

                if (item.TryGetProperty("value", out var value) && value.GetArrayLength() >= 2)
                {
                    var valStr = value[1].GetString();
                    if (double.TryParse(valStr, out var val))
                    {
                        result[instance] = val;
                    }
                }
            }
        }
        catch
        {
            // ignore parse errors
        }

        return result;
    }

    private async Task<bool> ProbePostgresAsync(CancellationToken ct)
    {
        try
        {
            var cs = configuration.GetConnectionString("AdminDb");
            if (string.IsNullOrWhiteSpace(cs))
            {
                return false;
            }

            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync(ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ProbeHttpAsync(string url, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(3);
            using var resp = await client.GetAsync(url, ct);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ProbeTcpAsync(string host, int port, CancellationToken ct)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<int> FetchActiveAlertsCountAsync(CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(3);
            using var resp = await client.GetAsync("http://alertmanager:9093/api/v2/alerts", ct);
            if (!resp.IsSuccessStatusCode)
            {
                return 0;
            }

            using var doc = await JsonDocument.ParseAsync(
                await resp.Content.ReadAsStreamAsync(ct),
                cancellationToken: ct
            );
            return doc.RootElement.GetArrayLength();
        }
        catch
        {
            return 0;
        }
    }

    private async Task PopulateConnectionsAsync(
        DashboardOverviewResponse response,
        CancellationToken ct
    )
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(3);
            using var resp = await client.GetAsync("http://twitch-core:8080/api/ServerStats", ct);
            if (!resp.IsSuccessStatusCode)
            {
                return;
            }

            using var doc = await JsonDocument.ParseAsync(
                await resp.Content.ReadAsStreamAsync(ct),
                cancellationToken: ct
            );
            var root = doc.RootElement;
            var result = root.TryGetProperty("result", out var r) ? r : root;
            if (result.TryGetProperty("isEventSubConnected", out var es))
            {
                response.Connections["eventSub"] = es.GetBoolean();
            }

            if (result.TryGetProperty("isTwitchChatConnected", out var tc))
            {
                response.Connections["twitchChat"] = tc.GetBoolean();
            }
        }
        catch
        {
            // ignore
        }
    }
}
