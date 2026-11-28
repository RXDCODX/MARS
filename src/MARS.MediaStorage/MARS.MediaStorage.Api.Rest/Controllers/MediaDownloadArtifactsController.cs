using System.Diagnostics.CodeAnalysis;
﻿using MARS.MediaStorage.Application.Downloads;
using MARS.MediaStorage.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MARS.MediaStorage.Api.Rest.Controllers;

[ApiController]
[Route("api/MediaDownloadJobs")]
[Authorize(Policy = "ServiceApiKey")]
public sealed class MediaDownloadArtifactsController(
    IMediaDownloadJobService downloadJobs,
    IWebHostEnvironment environment
) : ControllerBase
{
    [HttpGet("{jobId}/artifact")]
    public async Task<IActionResult> GetArtifact(string jobId, CancellationToken cancellationToken)
    {
        var job = Guid.TryParse(jobId, out var id)
            ? await downloadJobs.GetAsync(id, cancellationToken)
            : await downloadJobs.GetByShortIdAsync(jobId, cancellationToken);

        if (
            job is null
            || job.Status != MediaDownloadJobStatus.Completed
            || string.IsNullOrWhiteSpace(job.ArtifactStoragePath)
            || !System.IO.File.Exists(job.ArtifactStoragePath)
            || (job.ExpiresAtUtc.HasValue && job.ExpiresAtUtc.Value <= DateTimeOffset.UtcNow)
        )
        {
            return NotFound();
        }

        var root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "media-downloads"));
        var fullPath = Path.GetFullPath(job.ArtifactStoragePath);
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        var contentType = string.IsNullOrWhiteSpace(job.ArtifactMimeType)
            ? "application/octet-stream"
            : job.ArtifactMimeType;

        return PhysicalFile(fullPath, contentType, job.ArtifactFileName);
    }
}
