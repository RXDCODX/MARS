using MARS.Scoreboard.Data;
using MARS.Scoreboard.Grpc;
using MARS.Scoreboard.Hubs;
using MARS.Scoreboard.Services;
using MARS.Shared.Extensions;
using MARS.Shared.Grpc.Scoreboard;
using ScoreboardService = MARS.Scoreboard.Services.ScoreboardService;

namespace MARS.Scoreboard;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Scoreboard", "ScoreboardDb");

        builder.Services.AddMarsDbContext<ScoreboardDbContext>(
            builder.Configuration,
            "scoreboard",
            "ScoreboardDb"
        );
        builder.Services.AddScoped<ScoreboardService>();
        builder.AddMarsGrpcHosting();
        builder.Services.AddMarsEventBroadcaster<ScoreboardEvent>();
        // Хаб табло для браузера. AddMarsSignalR обязателен и не переносится в
        // AddMarsGrpcHosting: тот поднимает gRPC, а не SignalR.
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
        app.MapGrpcService<ScoreboardGrpcService>();
        app.MapHub<ScoreboardHub>("/hubs/scoreboard");
        app.MapControllers();
        app.MapGet("/", () => "MARS.Scoreboard is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
