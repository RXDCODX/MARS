using System.Diagnostics.CodeAnalysis;

namespace MARS.MediaStorage.Domain.Entities;

[ExcludeFromCodeCoverage]
public sealed class MediaDownloadJob
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Url { get; set; } = string.Empty;

    public string ActorId { get; set; } = string.Empty;

    public string Platform { get; set; } = string.Empty;

    public string ExecutionId { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public string CacheKey { get; set; } = string.Empty;

    public int PendingDeliveryCount { get; set; }

    public MediaDownloadJobStatus Status { get; set; } = MediaDownloadJobStatus.Pending;

    public string SourceKind { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? FailureCategory { get; set; }

    public string? FailureReason { get; set; }

    public string? ArtifactUrl { get; set; }

    public string? ArtifactFileName { get; set; }

    public string? ArtifactMimeType { get; set; }

    public MediaDownloadArtifactKind ArtifactKind { get; set; } = MediaDownloadArtifactKind.Unknown;

    public long ArtifactSizeBytes { get; set; }

    public double ArtifactDurationSeconds { get; set; }

    public int ArtifactWidth { get; set; }

    public int ArtifactHeight { get; set; }

    public bool Trimmed { get; set; }

    public string? Caption { get; set; }

    public int AttemptCount { get; set; }

    public long ProcessingTimeMs { get; set; }

    public string? Stage { get; set; }

    public string? Profile { get; set; }

    public string? ArtifactStoragePath { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? LeaseUntilUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public DateTimeOffset? ExpiresAtUtc { get; set; }
}
