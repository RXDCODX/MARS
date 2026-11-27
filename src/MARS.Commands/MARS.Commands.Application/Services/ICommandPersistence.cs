using System;
using MARS.Commands.Domain.Entities;

namespace MARS.Commands.Application.Services;

public interface ICommandExecutionRepository
{
    Task<CommandExecution?> GetByIdAsync(Guid executionId, CancellationToken cancellationToken);

    Task<CommandExecution?> GetByRequestIdAsync(string requestId, CancellationToken cancellationToken);

    Task<CommandExecution> CreateWithOutboxAsync(
        CommandExecution execution,
        MARS.Commands.Domain.Entities.CommandOutboxMessage outboxMessage,
        CancellationToken cancellationToken
    );

    Task<bool> TryClaimAsync(Guid executionId, DateTimeOffset leaseUntilUtc, CancellationToken cancellationToken);

    Task MarkRunningAsync(Guid executionId, CancellationToken cancellationToken);

    Task MarkJobRunningAsync(Guid executionId, string jobId, CancellationToken cancellationToken);

    Task MarkCompletedAsync(Guid executionId, string? errorCode, CancellationToken cancellationToken);

    Task MarkFailedAsync(Guid executionId, string? errorCode, string failureReason, CancellationToken cancellationToken);

    Task MarkNeedsReviewAsync(Guid executionId, string failureReason, CancellationToken cancellationToken);

    Task MarkCancelledAsync(Guid executionId, string? failureReason, CancellationToken cancellationToken);

    Task<IReadOnlyList<CommandExecution>> GetPendingByJobIdAsync(string jobId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CommandExecution>> GetStaleRunningAsync(TimeSpan olderThan, int batchSize, CancellationToken cancellationToken);
}

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