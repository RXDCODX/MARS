using MARS.CinemaQueue.Interfaces;
using MARS.CinemaQueue.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.CinemaQueue.Services;

public static class CinemaQueueServiceCollectionExtensions
{
    public static IServiceCollection AddCinemaQueueServices(this IServiceCollection services)
    {
        services.AddScoped<ICinemaQueueRepository, CinemaQueueRepository>();
        services.AddScoped<ICinemaQueueService, CinemaQueueService>();
        services.AddScoped<IKinopoiskService, KinopoiskService>();
        services.AddScoped<IMediaMetadataService, MediaMetadataService>();
        services.AddScoped<ITwitchCinemaQueueService, TwitchCinemaQueueService>();
        services.AddHostedService<CinemaQueueNotificationService>();

        return services;
    }
}
