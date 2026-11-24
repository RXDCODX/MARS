using MARS.Admin.Api.Contracts;
using MARS.Admin.Api.Dashboard;
using MARS.Admin.Api.Hubs;
using MARS.Admin.Domain.Entities;
using MARS.Admin.Infrastructure.Postgres.Data;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MARS.Admin.Api.Dashboard.Listeners;

public sealed class ChatMessageDeletedEventListener(
    IDbContextFactory<AdminDbContext> dbFactory,
    IHubContext<DashboardHub, IDashboardHubClient> hub,
    ILogger<ChatMessageDeletedEventListener> logger
) : MarsEventListener<ChatMessageDeletedEvent>
{
    public override async Task Handle(ChatMessageDeletedEvent message, CancellationToken ct)
    {
        await EventLogWriter.WriteAsync(
            dbFactory,
            hub,
            nameof(ChatMessageDeletedEvent),
            "twitch-core",
            null,
            message,
            logger,
            ct
        );
    }
}
