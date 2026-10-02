using MARS.Shared.Extensions;
using MARS.Shikimori.Data;
using MARS.Shikimori.Services;

namespace MARS.Shikimori;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Имя строки подключения обязано совпадать в AddMarsDefaults и
        // AddMarsDbContext: иначе readiness зелёный при недоступной базе.
        builder.AddMarsDefaults("MARS.Shikimori", "ShikimoriDb");

        builder.Services.AddMarsDbContext<ShikimoriDbContext>(
            builder.Configuration,
            "shikimori",
            "ShikimoriDb"
        );

        // Клиент Shikimori и его рейт-лимитер живут только здесь: во всех
        // остальных сервисах находятся HTTP-клиенты этого сервиса.
        builder.Services.Configure<ShikimoriClientOptions>(
            builder.Configuration.GetSection(ShikimoriClientOptions.SectionName)
        );
        builder.Services.AddSingleton<IShikimoriRateLimiter, ShikimoriRateLimiter>();
        builder.Services.AddSingleton<ShikimoriService>();
        builder.Services.AddSingleton<ShikimoriCatalog>();

        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapControllers();
        app.MapGet("/", () => "MARS.Shikimori is running");

        // Миграции применяются ДО старта хоста: иначе фоновые обращения
        // получат 42P01 — таблиц ещё нет.
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
