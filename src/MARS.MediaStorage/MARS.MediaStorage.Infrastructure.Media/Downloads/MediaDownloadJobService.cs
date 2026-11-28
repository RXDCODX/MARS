using System.Diagnostics.CodeAnalysis;
﻿using System;
using System.Security.Cryptography;
using System.Text;
using MARS.MediaStorage.Application.Downloads;
using MARS.MediaStorage.Domain.Entities;
using MARS.Shared.Contracts.MediaStorage;

namespace MARS.MediaStorage.Infrastructure.Media.Downloads;

[ExcludeFromCodeCoverage]
public sealed class MediaDownloadJobService(IMediaDownloadJobRepository repository)
    : IMediaDownloadJobService
{
    public async Task<MediaDownloadJob> CreateAsync(
        CreateDownloadJobRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var url = (request.Url ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL is required.", nameof(request));
        }

        var actorId = request.ActorId ?? string.Empty;
        var now = DateTimeOffset.UtcNow;
        if (
            await repository.CountRecentByActorAsync(actorId, now.AddHours(-1), cancellationToken)
            >= 5
        )
        {
            throw new InvalidOperationException("Слишком много задач загрузки. Попробуйте позже.");
        }

        if (await repository.CountActiveByActorAsync(actorId, cancellationToken) >= 1)
        {
            throw new InvalidOperationException("У вас уже выполняется загрузка.");
        }

        var cacheKey = ComputeCacheKey(url);
        var reusable = await repository.FindReusableAsync(cacheKey, now, cancellationToken);
        if (reusable is not null)
        {
            reusable.PendingDeliveryCount++;
            await repository.UpdateAsync(reusable, cancellationToken);
            return reusable;
        }

        var job = new MediaDownloadJob
        {
            Url = url,
            ActorId = actorId,
            Platform = request.Platform ?? string.Empty,
            ExecutionId = request.ExecutionId ?? string.Empty,
            CorrelationId = request.CorrelationId ?? string.Empty,
            Status = MediaDownloadJobStatus.Pending,
            SourceKind = ResolveSourceKind(url),
            Title = url,
            CacheKey = cacheKey,
            PendingDeliveryCount = 1,
        };

        return await repository.AddAsync(job, cancellationToken);
    }

    public Task<MediaDownloadJob?> GetAsync(
        Guid jobId,
        CancellationToken cancellationToken = default
    ) => repository.GetByIdAsync(jobId, cancellationToken);

    public Task<MediaDownloadJob?> GetByShortIdAsync(
        string shortJobId,
        CancellationToken cancellationToken = default
    ) => repository.GetByShortIdAsync(shortJobId, cancellationToken);

    public async Task<MediaDownloadJob?> CancelAsync(
        Guid jobId,
        CancellationToken cancellationToken = default
    )
    {
        var job = await repository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            return null;
        }

        if (
            job.Status
            is MediaDownloadJobStatus.Completed
                or MediaDownloadJobStatus.Failed
                or MediaDownloadJobStatus.Cancelled
        )
        {
            return job;
        }

        job.Status = MediaDownloadJobStatus.Cancelled;
        job.CompletedAtUtc = DateTimeOffset.UtcNow;
        await repository.UpdateAsync(job, cancellationToken);
        return job;
    }

    private static string ComputeCacheKey(string url)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string ResolveSourceKind(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "unknown";
        }

        if (url.Contains("soundcloud", StringComparison.OrdinalIgnoreCase))
        {
            return "soundcloud";
        }

        if (
            url.Contains("youtube", StringComparison.OrdinalIgnoreCase)
            || url.Contains("youtu.be", StringComparison.OrdinalIgnoreCase)
        )
        {
            return "youtube";
        }

        return "direct";
    }
}
