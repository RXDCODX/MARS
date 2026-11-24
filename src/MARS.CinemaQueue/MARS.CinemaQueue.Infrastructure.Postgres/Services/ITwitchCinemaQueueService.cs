using MARS.CinemaQueue.Application.Models;
using MARS.CinemaQueue.Application.Ports;
using MARS.CinemaQueue.Domain.Entities;
using MARS.CinemaQueue.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace MARS.CinemaQueue.Infrastructure.Postgres.Services;

public interface ITwitchCinemaQueueService
{
    Task<CinemaMediaItemDto?> HandleCinemaQueueRedemptionAsync(
        string userName,
        string userId,
        string userInput,
        string rewardTitle,
        CancellationToken cancellationToken = default
    );
}
