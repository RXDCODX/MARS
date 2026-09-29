using MARS.Scoreboard.Data;
using MARS.Scoreboard.Hubs;
using MARS.Scoreboard.Services;
using MARS.Shared.Extensions;

namespace MARS.Scoreboard;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Scoreboard");

        builder.Services.AddMarsDbContext<ScoreboardDbContext>(builder.Configuration, "scoreboard");
        builder.Services.AddScoped<ScoreboardService>();
        builder.Services.AddSignalR();
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
