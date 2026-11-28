using System;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Commands.Domain.Entities;

public sealed class CommandExecution
{
    public Guid Id { get; set; }

    public string RequestId { get; set; } = string.Empty;

    public string Platform { get; set; } = string.Empty;

    public string CommandName { get; set; } = string.Empty;

    public string CallerId { get; set; } = string.Empty;

    public string? CallerDisplayName { get; set; }

    public bool IsAdmin { get; set; }

    public string InputHash { get; set; } = string.Empty;

    public MARS.Commands.Domain.Enums.CommandExecutionStatus Status { get; set; }

    public DateTimeOffset AcceptedAtUtc { get; set; }

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? LeaseUntilUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public string? ErrorCode { get; set; }

    public string? FailureReason { get; set; }

    public bool NeedsReview { get; set; }

    public string? JobId { get; set; }

    public string? MediaStage { get; set; }

    public string? MediaProfile { get; set; }

    public long? ArtifactSizeBytes { get; set; }

    public int? ProcessingDurationMs { get; set; }

    public int? ArtifactKind { get; set; }

    public bool? Trimmed { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
