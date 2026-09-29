using MARS.Shared.Hubs;
using MARS.OBS.Services;
using MARS.Shared.Extensions;

namespace MARS.OBS;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.OBS");

        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<IObsService, HttpObsService>();
        builder.Services.AddSignalR();
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapHub<TelegramusHub>("/hubs/telegramus");
        app.MapControllers();
        app.MapGet("/", () => "MARS.OBS is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
