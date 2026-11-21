using MARS.CinemaQueue.Application.Ports;
using MARS.CinemaQueue.Application.Services;
using MARS.CinemaQueue.Infrastructure.Kinopoisk.Configuration;
using MARS.CinemaQueue.Infrastructure.Postgres.Data;
using MARS.CinemaQueue.Infrastructure.Postgres.Services;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Platform.Infrastructure.Persistence.Postgres.Extensions;

namespace MARS.CinemaQueue.Host;

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
        // Регистрации очереди кино живут в AddCinemaQueueServices: держать их здесь
        // значило бы завести второй источник правды, и lifetime поплыл бы в
        // одном месте, а проверялся бы в другом.
        builder.Services.AddCinemaQueueServices();
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
