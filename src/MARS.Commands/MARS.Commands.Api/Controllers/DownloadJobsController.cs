using MARS.Commands.Application.Services.Entitys;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.MediaStorage;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.Mvc;

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

[ApiController]
[Route("api/Commands/download-jobs")]
public sealed class DownloadJobsController(IMarsGrpcClient grpcClient) : ControllerBase
{
    [HttpGet("{jobId}")]
    public async Task<ActionResult<OperationResult<DownloadJobStatusDto>>> GetStatus(
        string jobId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var job = await grpcClient.MediaStorage.GetDownloadJobAsync(
                new GetDownloadJobRequest { JobId = jobId },
                cancellationToken
            );

            var dto = new DownloadJobStatusDto(
                job.JobId,
                job.ShortJobId,
                job.Status,
                job.SourceKind,
                job.Title,
                job.Stage,
                job.Profile,
                job.FailureCategory,
                job.FailureReason,
                job.ArtifactUrl,
                job.ArtifactFileName,
                job.ArtifactSizeBytes,
                job.ArtifactDurationSeconds,
                job.ArtifactWidth,
                job.ArtifactHeight,
                job.Trimmed,
                job.Caption,
                job.AttemptCount,
                job.ProcessingTimeMs,
                job.CreatedAtUtc,
                job.CompletedAtUtc
            );

            return Ok(OperationResult<DownloadJobStatusDto>.Ok(dto));
        }
        catch (Exception ex)
        {
            return Ok(OperationResult<DownloadJobStatusDto>.Fail(ex.Message));
        }
    }

    [HttpPost("{jobId}/cancel")]
    public async Task<ActionResult<OperationResult<DownloadJobStatusDto>>> Cancel(
        string jobId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var job = await grpcClient.MediaStorage.CancelDownloadJobAsync(
                new GetDownloadJobRequest { JobId = jobId },
                cancellationToken
            );

            var dto = new DownloadJobStatusDto(
                job.JobId,
                job.ShortJobId,
                job.Status,
                job.SourceKind,
                job.Title,
                job.Stage,
                job.Profile,
                job.FailureCategory,
                job.FailureReason,
                job.ArtifactUrl,
                job.ArtifactFileName,
                job.ArtifactSizeBytes,
                job.ArtifactDurationSeconds,
                job.ArtifactWidth,
                job.ArtifactHeight,
                job.Trimmed,
                job.Caption,
                job.AttemptCount,
                job.ProcessingTimeMs,
                job.CreatedAtUtc,
                job.CompletedAtUtc
            );

            return Ok(OperationResult<DownloadJobStatusDto>.Ok(dto));
        }
        catch (Exception ex)
        {
            return Ok(OperationResult<DownloadJobStatusDto>.Fail(ex.Message));
        }
    }
}
