using MARS.CinemaQueue.Application.Models;
using MARS.CinemaQueue.Application.Ports;
using MARS.CinemaQueue.Domain.Entities;
using Microsoft.Extensions.Hosting;

namespace MARS.CinemaQueue.Infrastructure.Postgres.Services;

public interface ICinemaQueueNotificationService
{
    Task CheckAndNotifyUnwatchedNextItemsAsync(CancellationToken cancellationToken = default);
}
