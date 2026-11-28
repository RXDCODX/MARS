using System.Diagnostics.CodeAnalysis;
﻿using MARS.MediaStorage.Application.Downloads;
using MARS.MediaStorage.Domain.Entities;
using MARS.MediaStorage.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace MARS.MediaStorage.Infrastructure.Postgres.Downloads;

[ExcludeFromCodeCoverage]
public sealed class MediaDownloadJobRepository(IDbContextFactory<MediaStorageDbContext> factory)
    : IMediaDownloadJobRepository
{
    public async Task<MediaDownloadJob> AddAsync(
        MediaDownloadJob job,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.MediaDownloadJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task<MediaDownloadJob?> GetByIdAsync(
        Guid jobId,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.MediaDownloadJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
    }

    public async Task<MediaDownloadJob?> GetByShortIdAsync(
        string shortJobId,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(shortJobId))
        {
            return null;
        }

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (Guid.TryParse(shortJobId, out var fullId))
        {
            return await db.MediaDownloadJobs.FirstOrDefaultAsync(j => j.Id == fullId, cancellationToken);
        }

        var candidates = await db.MediaDownloadJobs
            .Where(j => j.Id.ToString().StartsWith(shortJobId))
            .Take(2)
            .ToListAsync(cancellationToken);

        return candidates.Count == 1 ? candidates[0] : null;
    }

    public async Task<MediaDownloadJob?> FindReusableAsync(
        string cacheKey,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.MediaDownloadJobs.FirstOrDefaultAsync(
            j =>
                j.CacheKey == cacheKey
                && (
                    j.Status == MediaDownloadJobStatus.Pending
                    || j.Status == MediaDownloadJobStatus.Running
                    || (
                        j.Status == MediaDownloadJobStatus.Completed
                        && (j.ExpiresAtUtc == null || j.ExpiresAtUtc > nowUtc)
                    )
                ),
            cancellationToken
        );
    }

    public async Task<int> CountRecentByActorAsync(
        string actorId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.MediaDownloadJobs.CountAsync(
            j => j.ActorId == actorId && j.CreatedAtUtc >= sinceUtc,
            cancellationToken
        );
    }

    public async Task<int> CountActiveByActorAsync(
        string actorId,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.MediaDownloadJobs.CountAsync(
            j =>
                j.ActorId == actorId
                && (
                    j.Status == MediaDownloadJobStatus.Pending
                    || j.Status == MediaDownloadJobStatus.Running
                ),
            cancellationToken
        );
    }

    public async Task<MediaDownloadJob?> ClaimPendingAsync(
        DateTimeOffset nowUtc,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var job = await db
            .MediaDownloadJobs.Where(j => j.Status == MediaDownloadJobStatus.Pending)
            .OrderBy(j => j.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
        {
            return null;
        }

        job.Status = MediaDownloadJobStatus.Running;
        job.StartedAtUtc = nowUtc;
        job.LeaseUntilUtc = nowUtc.Add(leaseDuration);
        job.AttemptCount++;
        await db.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task<IReadOnlyList<MediaDownloadJob>> GetExpiredAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db
            .MediaDownloadJobs.Where(j => j.ExpiresAtUtc != null && j.ExpiresAtUtc <= nowUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkDeliveryCompletedAsync(
        Guid jobId,
        Guid executionId,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var job = await db.MediaDownloadJobs.FirstOrDefaultAsync(
            j => j.Id == jobId,
            cancellationToken
        );
        if (job is null)
        {
            return;
        }

        if (job.PendingDeliveryCount > 0)
        {
            job.PendingDeliveryCount--;
        }

        if (job.PendingDeliveryCount <= 0 && job.Status == MediaDownloadJobStatus.Completed)
        {
            job.ExpiresAtUtc = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        MediaDownloadJob job,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.MediaDownloadJobs.Update(job);
        await db.SaveChangesAsync(cancellationToken);
    }
}
