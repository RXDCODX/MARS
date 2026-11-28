using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Domain.Entities;
using MARS.Commands.Domain.Enums;
using MARS.Commands.Infrastructure.Postgres.Data;
using MARS.Shared.Contracts.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Commands.Host.Services;

public sealed class CommandOutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    ICommandService commandService,
    IMessageBus messageBus,
    ILogger<CommandOutboxDispatcher> logger
) : BackgroundService
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExecuteOutboxAsync(stoppingToken);
                await ProcessResponseOutboxAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Command outbox dispatch cycle failed.");
            }

            await Task.Delay(IdleDelay, stoppingToken);
        }
    }

    private async Task ProcessExecuteOutboxAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var executionRepository =
            scope.ServiceProvider.GetRequiredService<ICommandExecutionRepository>();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<ICommandOutboxRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<CommandsDbContext>();
        var responseSender = scope.ServiceProvider.GetRequiredService<ICommandResponseSender>();

        var messages = await outboxRepository.ClaimPendingAsync(
            CommandOutboxMessageType.ExecuteCommand,
            10,
            DateTimeOffset.UtcNow,
            LeaseDuration,
            cancellationToken
        );

        foreach (var message in messages)
        {
            CommandInvocation? invocation = null;
            try
            {
                invocation = JsonSerializer.Deserialize<CommandInvocation>(message.Payload);
                if (invocation is null)
                {
                    throw new InvalidOperationException("Execute outbox payload is empty.");
                }

                var claimed = await executionRepository.TryClaimAsync(
                    message.ExecutionId,
                    DateTimeOffset.UtcNow.Add(LeaseDuration),
                    cancellationToken
                );
                if (!claimed)
                {
                    await outboxRepository.MarkCompletedAsync(message.Id, cancellationToken);
                    continue;
                }

                await executionRepository.MarkRunningAsync(message.ExecutionId, cancellationToken);

                var result = await commandService.ExecuteCommandAsync(
                    invocation.CommandName,
                    invocation.Input,
                    ParsePlatform(invocation.Platform),
                    invocation.IsAdmin,
                    cancellationToken
                );

                var responsePayload = JsonSerializer.Serialize(
                    new CommandResponsePayload(
                        invocation.Platform,
                        invocation.ReplyTo,
                        invocation.SourceMessageId,
                        string.IsNullOrWhiteSpace(result.Text) ? string.Empty : result.Text,
                        result.Attachments
                    )
                );

                await using var transaction = await dbContext.Database.BeginTransactionAsync(
                    cancellationToken
                );

                if (!string.IsNullOrWhiteSpace(result.JobId))
                {
                    await executionRepository.MarkJobRunningAsync(
                        message.ExecutionId,
                        result.JobId,
                        cancellationToken
                    );
                }
                else
                {
                    await executionRepository.MarkCompletedAsync(
                        message.ExecutionId,
                        result.Success ? null : result.ErrorCode.ToString(),
                        cancellationToken
                    );
                }

                dbContext.CommandOutboxMessages.Add(
                    new CommandOutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        ExecutionId = message.ExecutionId,
                        MessageType = CommandOutboxMessageType.SendResponse,
                        Status = CommandOutboxStatus.Pending,
                        Payload = responsePayload,
                        CreatedAtUtc = DateTimeOffset.UtcNow,
                        NextAttemptAtUtc = DateTimeOffset.UtcNow,
                    }
                );

                await outboxRepository.MarkCompletedAsync(message.Id, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to execute command outbox message {OutboxId}.",
                    message.Id
                );
                var needsReview = invocation is not null && !invocation.IsIdempotent;
                if (needsReview)
                {
                    await executionRepository.MarkNeedsReviewAsync(
                        message.ExecutionId,
                        ex.Message,
                        cancellationToken
                    );
                }
                else
                {
                    await executionRepository.MarkFailedAsync(
                        message.ExecutionId,
                        null,
                        ex.Message,
                        cancellationToken
                    );
                }

                await outboxRepository.MarkFailedAsync(
                    message.Id,
                    ex.Message,
                    DateTimeOffset.UtcNow.AddSeconds(needsReview ? 3600 : 5),
                    cancellationToken
                );
            }
        }
    }

    private async Task ProcessResponseOutboxAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<ICommandOutboxRepository>();
        var executionRepository =
            scope.ServiceProvider.GetRequiredService<ICommandExecutionRepository>();
        var responseSender = scope.ServiceProvider.GetRequiredService<ICommandResponseSender>();

        var messages = await outboxRepository.ClaimPendingAsync(
            CommandOutboxMessageType.SendResponse,
            10,
            DateTimeOffset.UtcNow,
            LeaseDuration,
            cancellationToken
        );

        foreach (var message in messages)
        {
            try
            {
                var execution = await executionRepository.GetByIdAsync(
                    message.ExecutionId,
                    cancellationToken
                );
                if (execution is null)
                {
                    await outboxRepository.MarkCompletedAsync(message.Id, cancellationToken);
                    continue;
                }

                await responseSender.SendAsync(execution, message.Payload, cancellationToken);
                if (!string.IsNullOrWhiteSpace(execution.JobId))
                {
                    var payload = JsonSerializer.Deserialize<CommandResponsePayload>(
                        message.Payload
                    );
                    if (payload?.Attachments is { Count: > 0 })
                    {
                        await messageBus.PublishAsync(
                            new MediaDownloadDeliveryCompletedEvent
                            {
                                JobId = Guid.Parse(execution.JobId),
                                ExecutionId = execution.Id,
                                Succeeded = true,
                                CompletedAtUtc = DateTimeOffset.UtcNow,
                            }
                        );
                    }
                }

                await outboxRepository.MarkCompletedAsync(message.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to deliver command response outbox message {OutboxId}.",
                    message.Id
                );
                await outboxRepository.MarkFailedAsync(
                    message.Id,
                    ex.Message,
                    DateTimeOffset.UtcNow.AddSeconds(5),
                    cancellationToken
                );
            }
        }
    }

    private static MARS.Commands.Application.Services.Entitys.Platform ParsePlatform(
        string platform
    ) =>
        platform switch
        {
            "Telegram" => MARS.Commands.Application.Services.Entitys.Platform.Telegram,
            "Twitch" => MARS.Commands.Application.Services.Entitys.Platform.Twitch,
            "Discord" => MARS.Commands.Application.Services.Entitys.Platform.Discord,
            _ => MARS.Commands.Application.Services.Entitys.Platform.Api,
        };
}
