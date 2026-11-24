using System.Text.Json;
using MARS.Admin.Api.Contracts;
using MARS.Admin.Api.Hubs;
using MARS.Admin.Domain.Entities;
using MARS.Admin.Infrastructure.Postgres.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MARS.Admin.Api.Dashboard;

/// <summary>
/// Общий хелпер записи бизнес-события в журнал и пуш в SignalR-хаб дашборда.
/// </summary>
public static class EventLogWriter
{
    public static async Task WriteAsync(
        IDbContextFactory<AdminDbContext> dbFactory,
        IHubContext<DashboardHub, IDashboardHubClient> hub,
        string type,
        string sourceService,
        string? actorDisplayName,
        object? payload,
        ILogger logger,
        CancellationToken ct
    )
    {
        try
        {
            var entry = new EventLog
            {
                Type = type,
                SourceService = sourceService,
                OccurredAtUtc = DateTime.UtcNow,
                ActorDisplayName = actorDisplayName,
                PayloadJson = payload is null ? "{}" : JsonSerializer.Serialize(payload),
            };

            await using var db = await dbFactory.CreateDbContextAsync(ct);
            await db.EventLogs.AddAsync(entry, ct);
            await db.SaveChangesAsync(ct);

            await hub.Clients.All.ReceiveEvent(
                new DashboardEventDto
                {
                    Id = entry.Id,
                    Type = entry.Type,
                    SourceService = entry.SourceService,
                    OccurredAtUtc = entry.OccurredAtUtc,
                    ActorDisplayName = entry.ActorDisplayName,
                    PayloadJson = entry.PayloadJson,
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write dashboard event {Type}", type);
        }
    }
}
