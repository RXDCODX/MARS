using System;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services;
using MARS.Commands.Domain.Entities;
using MARS.Commands.Infrastructure.Postgres.Data;
using MARS.Platform.Infrastructure.Grpc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Commands.Host.Services;

public sealed class MediaDownloadReconciliationWorker(
    IServiceScopeFactory scopeFactory,
    IMarsGrpcClient grpcClient,
    MediaDownloadJobFinalizer finalizer,
    ILogger<MediaDownloadReconciliationWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var repository =
                    scope.ServiceProvider.GetRequiredService<ICommandExecutionRepository>();

                var stale = await repository.GetStaleRunningAsync(
                    TimeSpan.FromMinutes(1),
                    20,
                    stoppingToken
                );
                foreach (var execution in stale)
                {
                    if (string.IsNullOrWhiteSpace(execution.JobId))
                    {
                        continue;
                    }

                    if (!Guid.TryParse(execution.JobId, out var jobId))
                    {
                        continue;
                    }

                    try
                    {
                        var job = await grpcClient.MediaStorage.GetDownloadJobAsync(
                            new MARS.Shared.Contracts.MediaStorage.GetDownloadJobRequest
                            {
                                JobId = execution.JobId,
                            },
                            stoppingToken
                        );

                        if (job.Status is 2 or 3 or 4)
                        {
                            await finalizer.FinalizeFromJobAsync(jobId, job, stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(
                            ex,
                            "Reconciliation failed for execution {ExecutionId}",
                            execution.Id
                        );
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Media download reconciliation cycle failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
