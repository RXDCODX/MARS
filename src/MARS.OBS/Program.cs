using MARS.OBS.Services;
using MARS.Shared.Extensions;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Grpc.Services;
using MARS.Shared.Grpc.Telegramus;

namespace MARS.OBS;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.OBS");

        builder.AddMarsGrpcHosting();
        builder.Services.AddMarsEventBroadcaster<TelegramusEvent>();
        builder.Services.AddSingleton<ITelegramusNotifier, TelegramusNotifier>();
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<IObsService, HttpObsService>();
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapGrpcService<TelegramusGrpcService>();
        app.MapControllers();
        app.MapGet("/", () => "MARS.OBS is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
