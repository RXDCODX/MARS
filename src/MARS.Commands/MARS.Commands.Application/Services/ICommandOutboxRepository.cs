using System;
using MARS.Commands.Domain.Entities;
using MARS.Commands.Domain.Enums;

namespace MARS.Commands.Application.Services;

public interface ICommandOutboxRepository
{
    Task<IReadOnlyList<MARS.Commands.Domain.Entities.CommandOutboxMessage>> ClaimPendingAsync(
        MARS.Commands.Domain.Enums.CommandOutboxMessageType messageType,
        int batchSize,
        DateTimeOffset nowUtc,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken
    );

    Task MarkCompletedAsync(Guid outboxId, CancellationToken cancellationToken);

    Task MarkFailedAsync(Guid outboxId, string error, DateTimeOffset nextAttemptUtc, CancellationToken cancellationToken);
}