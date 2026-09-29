using MARS.MediaStorage.DataBaseContext;
using MARS.MediaStorage.Services;
using MARS.MediaStorage.Services.Media;
using MARS.MediaStorage.Services.PyroAlerts;
using MARS.Shared.Extensions;

namespace MARS.MediaStorage;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.MediaStorage");

        // DbContext
        builder.Services.AddMarsDbContext<MediaStorageDbContext>(
            builder.Configuration,
            "mediastorage"
        );

        // Media services
        builder.Services.AddScoped<IMediaFileStorageService, WebRootMediaFileStorageService>();
        builder.Services.AddScoped<IMediaInspector, FfprobeMediaInspector>();
        builder.Services.AddScoped<IMediaTranscoder, MediaTranscoder>();

        // PyroAlerts services
        builder.Services.AddScoped<PyroAlertsHelper>();
        builder.Services.AddScoped<PyroAlertsHandler>();

        // RandomMeme service
        builder.Services.AddScoped<IRandomMemeService, RandomMemeService>();

        // Controllers
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMarsDefaults();

        app.MapControllers();

        app.MapGet("/", () => "MARS.MediaStorage is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
