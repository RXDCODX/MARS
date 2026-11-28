using System.Diagnostics.CodeAnalysis;
namespace MARS.Commands.Api.Controllers;

public sealed record DownloadJobStatusDto(
    string JobId,
    string ShortJobId,
    int Status,
    string SourceKind,
    string Title,
    string Stage,
    string Profile,
    string FailureCategory,
    string FailureReason,
    string ArtifactUrl,
    string ArtifactFileName,
    long ArtifactSizeBytes,
    double ArtifactDurationSeconds,
    int ArtifactWidth,
    int ArtifactHeight,
    bool Trimmed,
    string Caption,
    int AttemptCount,
    long ProcessingTimeMs,
    string CreatedAtUtc,
    string CompletedAtUtc
);