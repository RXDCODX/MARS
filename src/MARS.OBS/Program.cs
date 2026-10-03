using MARS.OBS.Services;
using MARS.Shared.Extensions;

// Хаб оверлея и broadcaster TelegramusEvent из MARS.OBS убраны вместе с
// TestAlertsController. Сервис держал свой экземпляр GrpcEventBroadcaster, и
// отладочная отправка алертов попадала только в него: до подписки gRPC и
// подписки SignalR оверлея получали два непересекающихся набора событий.
// Оверлей теперь единственный источник подписки — MARS.Alerts.

namespace MARS.OBS;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.OBS");

        builder.AddMarsGrpcHosting();
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<IObsService, HttpObsService>();
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapControllers();
        app.MapGet("/", () => "MARS.OBS is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
