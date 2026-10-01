using MARS.Videos365.Configuration;
using MARS.Videos365.Data;
using MARS.Videos365.Services;
using MARS.Shared.Extensions;
using Microsoft.Extensions.Options;

namespace MARS.Videos365;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Videos365", "Videos365Db");

        builder.Services.AddMarsDbContext<Videos365DbContext>(
            builder.Configuration,
            "videos365",
            "Videos365Db"
        );

        builder.Services.AddHttpClient();

        builder.Services.Configure<Config365>(builder.Configuration.GetSection(Config365.SectionName));

        builder.Services.AddHostedService<Worker365>();

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapGet("/", () => "MARS.Videos365 is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
