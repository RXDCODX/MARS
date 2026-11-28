using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Domain.Entities;
using MARS.Commands.Domain.Enums;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Commands.Application.Services;

[ExcludeFromCodeCoverage]
public sealed class CommandExecutionLedger(
    ICommandExecutionRepository executionRepository
) : ICommandExecutionLedger
{
    public async Task<CommandExecution> AcceptAsync(CommandInvocation invocation, CancellationToken cancellationToken)
    {
        var requestId = string.IsNullOrWhiteSpace(invocation.RequestId)
            ? Guid.NewGuid().ToString("N")
            : invocation.RequestId;

        var inputHash = ComputeHash(invocation.Input);
        var existing = await executionRepository.GetByRequestIdAsync(requestId, cancellationToken);
        if (existing is not null)
        {
            if (existing.InputHash != inputHash
                || existing.CommandName != invocation.CommandName
                || existing.Platform != invocation.Platform)
            {
                throw new InvalidOperationException(
                    $"RequestId '{requestId}' already refers to a different command request."
                );
            }

            return existing;
        }

        var execution = new CommandExecution
        {
            Id = Guid.NewGuid(),
            RequestId = requestId,
            Platform = invocation.Platform,
            CommandName = invocation.CommandName,
            CallerId = invocation.CallerId,
            CallerDisplayName = invocation.CallerDisplayName,
            IsAdmin = invocation.IsAdmin,
            InputHash = inputHash,
            Status = CommandExecutionStatus.Accepted,
            AcceptedAtUtc = DateTimeOffset.UtcNow,
        };

        var outbox = new CommandOutboxMessage
        {
            Id = Guid.NewGuid(),
            ExecutionId = execution.Id,
            MessageType = CommandOutboxMessageType.ExecuteCommand,
            Status = CommandOutboxStatus.Pending,
            Payload = JsonSerializer.Serialize(invocation with { RequestId = requestId }),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            NextAttemptAtUtc = DateTimeOffset.UtcNow,
        };

        return await executionRepository.CreateWithOutboxAsync(execution, outbox, cancellationToken);
    }

    public Task<CommandExecution?> GetStatusAsync(Guid executionId, CancellationToken cancellationToken) =>
        executionRepository.GetByIdAsync(executionId, cancellationToken);

    private static string ComputeHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}