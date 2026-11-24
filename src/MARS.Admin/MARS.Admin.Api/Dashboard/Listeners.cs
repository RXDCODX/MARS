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

public sealed class ChatEventListener(
    IDbContextFactory<AdminDbContext> dbFactory,
    IHubContext<DashboardHub, IDashboardHubClient> hub,
    ILogger<ChatEventListener> logger
) : MarsEventListener<ChatEventMessageEvent>
{
    public override async Task Handle(ChatEventMessageEvent message, CancellationToken ct)
    {
        await EventLogWriter.WriteAsync(
            dbFactory,
            hub,
            nameof(ChatEventMessageEvent),
            "twitch-core",
            message.UserName,
            message,
            logger,
            ct
        );
    }
}

public sealed class CommandExecuteEventListener(
    IDbContextFactory<AdminDbContext> dbFactory,
    IHubContext<DashboardHub, IDashboardHubClient> hub,
    ILogger<CommandExecuteEventListener> logger
) : MarsEventListener<CommandExecuteRequestedEvent>
{
    public override async Task Handle(CommandExecuteRequestedEvent message, CancellationToken ct)
    {
        await EventLogWriter.WriteAsync(
            dbFactory,
            hub,
            nameof(CommandExecuteRequestedEvent),
            "commands",
            message.UserName,
            message,
            logger,
            ct
        );
    }
}

public sealed class RewardInputChatEventListener(
    IDbContextFactory<AdminDbContext> dbFactory,
    IHubContext<DashboardHub, IDashboardHubClient> hub,
    ILogger<RewardInputChatEventListener> logger
) : MarsEventListener<RewardInputChatMessageEvent>
{
    public override async Task Handle(RewardInputChatMessageEvent message, CancellationToken ct)
    {
        await EventLogWriter.WriteAsync(
            dbFactory,
            hub,
            nameof(RewardInputChatMessageEvent),
            "twitch-core",
            message.Message.UserName,
            message,
            logger,
            ct
        );
    }
}

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

public sealed class ChatSendEventListener(
    IDbContextFactory<AdminDbContext> dbFactory,
    IHubContext<DashboardHub, IDashboardHubClient> hub,
    ILogger<ChatSendEventListener> logger
) : MarsEventListener<ChatSendEvent>
{
    public override async Task Handle(ChatSendEvent message, CancellationToken ct)
    {
        await EventLogWriter.WriteAsync(
            dbFactory,
            hub,
            nameof(ChatSendEvent),
            "twitch-core",
            null,
            message,
            logger,
            ct
        );
    }
}

public sealed class RewardRedeemedEventListener(
    IDbContextFactory<AdminDbContext> dbFactory,
    IHubContext<DashboardHub, IDashboardHubClient> hub,
    ILogger<RewardRedeemedEventListener> logger
) : MarsEventListener<RewardRedeemedEvent>
{
    public override async Task Handle(RewardRedeemedEvent message, CancellationToken ct)
    {
        await EventLogWriter.WriteAsync(
            dbFactory,
            hub,
            nameof(RewardRedeemedEvent),
            "alerts",
            null,
            message,
            logger,
            ct
        );
    }
}
