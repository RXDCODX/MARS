using System;

namespace MARS.Commands.Domain.Entities;

public sealed class CommandOutboxMessage
{
    public Guid Id { get; set; }

    public Guid ExecutionId { get; set; }

    public MARS.Commands.Domain.Enums.CommandOutboxMessageType MessageType { get; set; }

    public MARS.Commands.Domain.Enums.CommandOutboxStatus Status { get; set; }

    public string Payload { get; set; } = string.Empty;

    public int AttemptCount { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset NextAttemptAtUtc { get; set; }

    public DateTimeOffset? LeaseUntilUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public string? Error { get; set; }

    public byte[] RowVersion { get; set; } = [];
}