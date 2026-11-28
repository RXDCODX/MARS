using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Domain.Entities;
using MARS.Commands.Domain.Enums;
using MARS.Commands.Infrastructure.Postgres.Data;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.MediaStorage;
using MARS.Shared.Contracts.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProtoMedia = MARS.Shared.Contracts.MediaStorage;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Commands.Host.Services;

public sealed class MediaDownloadJobFinalizer(
    IServiceScopeFactory scopeFactory,
    ILogger<MediaDownloadJobFinalizer> logger
)
{
    public async Task FinalizeFromEventAsync(
        MediaDownloadJobCompletedEvent message,
        CancellationToken cancellationToken
    )
    {
        if (message is null || message.JobId == Guid.Empty)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var executionRepository =
            scope.ServiceProvider.GetRequiredService<ICommandExecutionRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<CommandsDbContext>();

        var executions = await executionRepository.GetPendingByJobIdAsync(
            message.JobId.ToString("N"),
            cancellationToken
        );
        if (executions.Count == 0)
        {
            return;
        }

        foreach (var execution in executions)
        {
            await FinalizeExecutionAsync(execution, message, dbContext, cancellationToken);
        }
    }

    public async Task FinalizeFromJobAsync(
        Guid jobId,
        ProtoMedia.DownloadJobResponse job,
        CancellationToken cancellationToken
    )
    {
        var message = new MediaDownloadJobCompletedEvent
        {
            JobId = jobId,
            Succeeded = job.Status == 2,
            FailureCategory = job.FailureCategory,
            FailureReason = job.FailureReason,
            ArtifactUrl = job.ArtifactUrl,
            FileName = job.ArtifactFileName,
            MimeType = job.ArtifactMimeType,
            Kind = job.ArtifactKind,
            SizeBytes = job.ArtifactSizeBytes,
            DurationSeconds = job.ArtifactDurationSeconds,
            Width = job.ArtifactWidth,
            Height = job.ArtifactHeight,
            Trimmed = job.Trimmed,
            Caption = job.Caption,
            CompletedAtUtc = string.IsNullOrWhiteSpace(job.CompletedAtUtc)
                ? DateTimeOffset.UtcNow
                : DateTimeOffset.Parse(
                    job.CompletedAtUtc,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind
                ),
        };

        if (job.Status == 4)
        {
            message.FailureCategory = "cancelled";
        }

        await FinalizeFromEventAsync(message, cancellationToken);
    }

    private async Task FinalizeExecutionAsync(
        CommandExecution execution,
        MediaDownloadJobCompletedEvent message,
        CommandsDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                cancellationToken
            );

            execution.MediaStage = message.Succeeded ? "completed" : "failed";
            execution.ArtifactSizeBytes = message.SizeBytes > 0 ? message.SizeBytes : null;
            execution.ProcessingDurationMs =
                message.CompletedAtUtc > execution.StartedAtUtc
                    ? (int)
                        (message.CompletedAtUtc - execution.StartedAtUtc!.Value).TotalMilliseconds
                    : null;
            execution.ArtifactKind = message.Kind;
            execution.Trimmed = message.Trimmed;
            execution.CompletedAtUtc = message.CompletedAtUtc;

            var text = message.Succeeded
                ? (string.IsNullOrWhiteSpace(message.Caption) ? "✅ Файл готов." : message.Caption!)
                : $"❌ Не удалось скачать файл: {message.FailureCategory ?? "ошибка обработки"} — {message.FailureReason ?? "неизвестная причина"}";

            if (message.Succeeded)
            {
                execution.Status = CommandExecutionStatus.Completed;
                execution.ErrorCode = null;
                execution.FailureReason = null;
            }
            else if (
                string.Equals(
                    message.FailureCategory,
                    "cancelled",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                execution.Status = CommandExecutionStatus.Cancelled;
                execution.FailureReason = message.FailureReason;
                text = "❌ Загрузка отменена.";
            }
            else
            {
                execution.Status = CommandExecutionStatus.Failed;
                execution.ErrorCode = "Failed";
                execution.FailureReason = message.FailureReason;
            }

            var attachments = string.IsNullOrWhiteSpace(message.ArtifactUrl)
                ? null
                : new[]
                {
                    new CommandAttachment(
                        MapKind(message.Kind),
                        message.ArtifactUrl,
                        message.Caption,
                        message.FileName,
                        message.MimeType,
                        message.DurationSeconds,
                        message.Width,
                        message.Height
                    ),
                };

            var payload = JsonSerializer.Serialize(
                new CommandResponsePayload(
                    execution.Platform,
                    null,
                    null,
                    text,
                    message.Succeeded ? attachments : null
                )
            );

            dbContext.CommandOutboxMessages.Add(
                new CommandOutboxMessage
                {
                    Id = Guid.NewGuid(),
                    ExecutionId = execution.Id,
                    MessageType = CommandOutboxMessageType.SendResponse,
                    Status = CommandOutboxStatus.Pending,
                    Payload = payload,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    NextAttemptAtUtc = DateTimeOffset.UtcNow,
                }
            );

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to finalize download execution {ExecutionId} for job {JobId}",
                execution.Id,
                message.JobId
            );
        }
    }

    private static CommandAttachmentKind MapKind(int? kind) =>
        kind switch
        {
            1 => CommandAttachmentKind.Image,
            2 => CommandAttachmentKind.Video,
            3 => CommandAttachmentKind.Audio,
            _ => CommandAttachmentKind.File,
        };
}
