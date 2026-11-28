using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services;
using MARS.Commands.Domain.Entities;
using MARS.Commands.Domain.Enums;
using MARS.Commands.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace MARS.Commands.Infrastructure.Postgres.Repositories;

[ExcludeFromCodeCoverage]
public sealed class CommandOutboxRepository(CommandsDbContext dbContext) : ICommandOutboxRepository
{
    public async Task<IReadOnlyList<CommandOutboxMessage>> ClaimPendingAsync(
        CommandOutboxMessageType messageType,
        int batchSize,
        DateTimeOffset nowUtc,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken
        );

        var messages = await dbContext
            .CommandOutboxMessages.Where(x =>
                x.MessageType == messageType
                && x.Status == CommandOutboxStatus.Pending
                && x.NextAttemptAtUtc <= nowUtc
            )
            .OrderBy(x => x.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var leaseUntil = nowUtc.Add(leaseDuration);
        foreach (var message in messages)
        {
            message.Status = CommandOutboxStatus.InProgress;
            message.LeaseUntilUtc = leaseUntil;
            message.AttemptCount++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return messages;
    }

    public async Task MarkCompletedAsync(Guid outboxId, CancellationToken cancellationToken)
    {
        var message = await dbContext.CommandOutboxMessages.FirstAsync(
            x => x.Id == outboxId,
            cancellationToken
        );
        message.Status = CommandOutboxStatus.Completed;
        message.CompletedAtUtc = DateTimeOffset.UtcNow;
        message.LeaseUntilUtc = null;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid outboxId,
        string error,
        DateTimeOffset nextAttemptUtc,
        CancellationToken cancellationToken
    )
    {
        var message = await dbContext.CommandOutboxMessages.FirstAsync(
            x => x.Id == outboxId,
            cancellationToken
        );
        message.Status = CommandOutboxStatus.Failed;
        message.Error = error;
        message.NextAttemptAtUtc = nextAttemptUtc;
        message.LeaseUntilUtc = null;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
