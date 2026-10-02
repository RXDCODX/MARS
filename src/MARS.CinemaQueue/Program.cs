using MARS.CinemaQueue.Configuration;
using MARS.CinemaQueue.Data;
using MARS.CinemaQueue.Interfaces;
using MARS.CinemaQueue.Repositories;
using MARS.CinemaQueue.Services;
using MARS.Shared.Extensions;

namespace MARS.CinemaQueue;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.CinemaQueue", "CinemaDb");

        builder.Services.AddMarsDbContext<CinemaDbContext>(
            builder.Configuration,
            "cinema",
            "CinemaDb"
        );

        // Options
        builder.Services.Configure<KinopoiskConfiguration>(
            builder.Configuration.GetSection(KinopoiskConfiguration.SectionName)
        );

        // Services
        builder.Services.AddScoped<ICinemaQueueRepository, CinemaQueueRepository>();
        builder.Services.AddScoped<ICinemaQueueService, CinemaQueueService>();
        builder.Services.AddScoped<IKinopoiskService, KinopoiskService>();
        builder.Services.AddScoped<IMediaMetadataService, MediaMetadataService>();
        builder.Services.AddScoped<ITwitchCinemaQueueService, TwitchCinemaQueueService>();
        builder.Services.AddHostedService<CinemaQueueNotificationService>();
        builder.Services.AddHttpClient();
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapControllers();
        app.MapGet("/", () => "MARS.CinemaQueue is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
