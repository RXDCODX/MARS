using System;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services;
using MARS.Commands.Domain.Entities;
using MARS.Commands.Domain.Enums;
using MARS.Commands.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace MARS.Commands.Infrastructure.Postgres.Repositories;

public sealed class CommandExecutionRepository(CommandsDbContext dbContext) : ICommandExecutionRepository
{
    public Task<CommandExecution?> GetByIdAsync(Guid executionId, CancellationToken cancellationToken) =>
        dbContext.CommandExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == executionId, cancellationToken);

    public Task<CommandExecution?> GetByRequestIdAsync(string requestId, CancellationToken cancellationToken) =>
        dbContext.CommandExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RequestId == requestId, cancellationToken);

    public async Task<CommandExecution> CreateWithOutboxAsync(
        CommandExecution execution,
        CommandOutboxMessage outboxMessage,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.CommandExecutions.Add(execution);
        dbContext.CommandOutboxMessages.Add(outboxMessage);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return execution;
    }

    public async Task<bool> TryClaimAsync(Guid executionId, DateTimeOffset leaseUntilUtc, CancellationToken cancellationToken)
    {
        var execution = await dbContext.CommandExecutions
            .FirstOrDefaultAsync(x => x.Id == executionId, cancellationToken);

        if (execution is null)
        {
            return false;
        }

        if (execution.Status is not (CommandExecutionStatus.Accepted or CommandExecutionStatus.Failed))
        {
            return false;
        }

        if (execution.Status == CommandExecutionStatus.Failed && execution.NeedsReview)
        {
            return false;
        }

        execution.Status = CommandExecutionStatus.Running;
        execution.StartedAtUtc ??= DateTimeOffset.UtcNow;
        execution.LeaseUntilUtc = leaseUntilUtc;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task MarkRunningAsync(Guid executionId, CancellationToken cancellationToken)
    {
        var execution = await dbContext.CommandExecutions
            .FirstAsync(x => x.Id == executionId, cancellationToken);
        execution.Status = CommandExecutionStatus.Running;
        execution.StartedAtUtc ??= DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkJobRunningAsync(Guid executionId, string jobId, CancellationToken cancellationToken)
    {
        var execution = await dbContext.CommandExecutions
            .FirstAsync(x => x.Id == executionId, cancellationToken);
        execution.Status = CommandExecutionStatus.Running;
        execution.JobId = jobId;
        execution.StartedAtUtc ??= DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkCancelledAsync(Guid executionId, string? failureReason, CancellationToken cancellationToken)
    {
        var execution = await dbContext.CommandExecutions
            .FirstAsync(x => x.Id == executionId, cancellationToken);
        execution.Status = CommandExecutionStatus.Cancelled;
        execution.FailureReason = failureReason;
        execution.CompletedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<CommandExecution>> GetPendingByJobIdAsync(string jobId, CancellationToken cancellationToken) =>
        dbContext.CommandExecutions
            .Where(x => x.JobId == jobId && x.Status == CommandExecutionStatus.Running)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<CommandExecution>)t.Result, cancellationToken);

    public Task<IReadOnlyList<CommandExecution>> GetStaleRunningAsync(TimeSpan olderThan, int batchSize, CancellationToken cancellationToken)
    {
        var threshold = DateTimeOffset.UtcNow - olderThan;
        return dbContext.CommandExecutions
            .Where(x => x.Status == CommandExecutionStatus.Running && x.JobId != null && x.StartedAtUtc != null && x.StartedAtUtc < threshold)
            .OrderBy(x => x.StartedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<CommandExecution>)t.Result, cancellationToken);
    }

    public async Task MarkCompletedAsync(Guid executionId, string? errorCode, CancellationToken cancellationToken)
    {
        var execution = await dbContext.CommandExecutions
            .FirstAsync(x => x.Id == executionId, cancellationToken);
        execution.Status = CommandExecutionStatus.Completed;
        execution.ErrorCode = errorCode;
        execution.CompletedAtUtc = DateTimeOffset.UtcNow;
        execution.NeedsReview = false;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid executionId, string? errorCode, string failureReason, CancellationToken cancellationToken)
    {
        var execution = await dbContext.CommandExecutions
            .FirstAsync(x => x.Id == executionId, cancellationToken);
        execution.Status = CommandExecutionStatus.Failed;
        execution.ErrorCode = errorCode;
        execution.FailureReason = failureReason;
        execution.CompletedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkNeedsReviewAsync(Guid executionId, string failureReason, CancellationToken cancellationToken)
    {
        var execution = await dbContext.CommandExecutions
            .FirstAsync(x => x.Id == executionId, cancellationToken);
        execution.Status = CommandExecutionStatus.NeedsReview;
        execution.FailureReason = failureReason;
        execution.NeedsReview = true;
        execution.CompletedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}