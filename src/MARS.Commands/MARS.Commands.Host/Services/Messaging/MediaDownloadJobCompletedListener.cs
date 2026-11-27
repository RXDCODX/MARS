using System;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Host.Services;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Commands.Host.Services.Messaging;

public sealed class MediaDownloadJobCompletedListener(
    MediaDownloadJobFinalizer finalizer,
    ILogger<MediaDownloadJobCompletedListener> logger
) : MarsEventListener<MediaDownloadJobCompletedEvent>
{
    public override async Task Handle(MediaDownloadJobCompletedEvent message, CancellationToken cancellationToken)
    {
        try
        {
            await finalizer.FinalizeFromEventAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle media download completion event for job {JobId}", message?.JobId);
        }
    }
}