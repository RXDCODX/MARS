using System;

namespace MARS.Shared.Contracts.Messaging;

public sealed class MediaDownloadDeliveryCompletedEvent : IMarsEventMessage
{
    public Guid JobId { get; set; }

    public Guid ExecutionId { get; set; }

    public bool Succeeded { get; set; }

    public string? FailureReason { get; set; }

    public DateTimeOffset CompletedAtUtc { get; set; }
}