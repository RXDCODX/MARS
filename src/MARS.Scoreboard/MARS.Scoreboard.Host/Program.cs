using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Platform.Infrastructure.Persistence.Postgres.Extensions;
using MARS.Platform.Infrastructure.Web.AspNetCore.Extensions;
using MARS.Scoreboard.Infrastructure.Postgres.Data;
using MARS.Scoreboard.Api.Hubs;
using MARS.Scoreboard.Application.Ports;
using MARS.Scoreboard.Infrastructure.Postgres.Services;
using MARS.Shared.Contracts.Scoreboard;
using ScoreboardService = MARS.Scoreboard.Infrastructure.Postgres.Services.ScoreboardService;

namespace MARS.Scoreboard.Host;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Scoreboard", "ScoreboardDb");

        // Второй Swagger-документ с описанием SignalR-хабов: генератор клиента
        // берёт из него обёртки hubs/* (scoreboard).
        builder.AddMarsHubsSwagger("MARS.Scoreboard", typeof(ScoreboardHub).Assembly);

        builder.Services.AddMarsDbContext<ScoreboardDbContext>(
            builder.Configuration,
            "scoreboard",
            "ScoreboardDb"
        );
        builder.Services.AddScoped<IScoreboardService, ScoreboardService>();
        builder.Services.AddMarsEventBroadcaster<ScoreboardEvent>();
        // Хаб табло для браузера. AddMarsSignalR задаёт формат на проводе хабов:
        // camelCase и перечисления именами.
        //
        // Именно AddMarsSignalR, а не сырой AddSignalR: формат на проводе хабов
        // задаётся там — camelCase и перечисления именами. Со сырым
        // перечисления ехали бы числами, и клиент, который сравнивает
        // status === "playing", не сходился бы никогда.
        builder.Services.AddMarsSignalR();
        builder.Services.AddHostedService<ScoreboardHubRelay>();
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapHub<ScoreboardHub>("/hubs/scoreboard");
        app.MapControllers();
        app.MapGet("/", () => "MARS.Scoreboard is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
