using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MARS.MediaStorage.Application.Downloads;
using MARS.MediaStorage.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.MediaStorage.Infrastructure.Media.Downloads;

[ExcludeFromCodeCoverage]
public sealed class MediaDownloadJobWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<MediaDownloadJobWorker> logger
) : BackgroundService
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var repository =
                    scope.ServiceProvider.GetRequiredService<IMediaDownloadJobRepository>();
                var processor =
                    scope.ServiceProvider.GetRequiredService<IMediaDownloadJobProcessor>();

                var job = await repository.ClaimPendingAsync(
                    DateTimeOffset.UtcNow,
                    LeaseDuration,
                    stoppingToken
                );
                if (job is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                try
                {
                    await processor.ProcessAsync(job, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Media download job {JobId} crashed", job.Id);
                    job.Status = MediaDownloadJobStatus.Failed;
                    job.FailureCategory = "processing";
                    job.FailureReason = ex.Message;
                    job.CompletedAtUtc = DateTimeOffset.UtcNow;
                    await repository.UpdateAsync(job, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Media download worker cycle failed");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
