using Microsoft.AspNetCore.Http.Features;
using MARS.MediaStorage.DataBaseContext;
using MARS.MediaStorage.Services;
using MARS.MediaStorage.Services.Git;
using MARS.MediaStorage.Services.Media;
using MARS.MediaStorage.Services.PyroAlerts;
using MARS.MediaStorage.Services.Storage;
using MARS.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace MARS.MediaStorage;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.MediaStorage", "MediaStorageDb");

        // DbContext
        builder.Services.AddMarsDbContext<MediaStorageDbContext>(
            builder.Configuration,
            "mediastorage",
            "MediaStorageDb"
        );

        // Media services
        builder.Services.AddScoped<IMediaFileStorageService, WebRootMediaFileStorageService>();
        builder.Services.AddScoped<IMediaInspector, FfprobeMediaInspector>();
        builder.Services.AddScoped<IMediaTranscoder, MediaTranscoder>();

        // Git-синхронизация wwwroot. Выключена по умолчанию: при Enabled=false
        // ни одна git-команда не выполняется. Рабочая копия — WebRootPath,
        // метаданные git — в отдельном томе (GitDataDirectory), чтобы переживали
        // пересборку образа.
        builder.Services.Configure<MediaGitOptions>(
            builder.Configuration.GetSection(MediaGitOptions.SectionName)
        );
        // IOptions<T> сам по себе не регистрирует и сам T, а MediaGitInitializer
        // принимает MediaGitOptions напрямую.
        builder.Services.AddSingleton(provider =>
            provider.GetRequiredService<IOptions<MediaGitOptions>>().Value
        );
        builder.Services.AddSingleton<IGitCommandExecutor, GitCommandExecutor>();
        builder.Services.AddSingleton<IMediaGitService>(provider => new MediaGitService(
            provider.GetRequiredService<IOptions<MediaGitOptions>>().Value,
            provider.GetRequiredService<IGitCommandExecutor>(),
            provider.GetRequiredService<IWebHostEnvironment>().WebRootPath
        ));
        builder.Services.AddHostedService<MediaGitInitializer>();

        // Файловое хранилище: индексация wwwroot, мягкое удаление, массовые
        // операции. Метаданные лежат в отдельной таблице MediaEntries, общий
        // контракт алертов MediaInfo не трогаем.
        builder.Services.Configure<MediaStorageOptions>(
            builder.Configuration.GetSection(MediaStorageOptions.SectionName)
        );
        builder.Services.AddSingleton(provider =>
            provider.GetRequiredService<IOptions<MediaStorageOptions>>().Value
        );
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IMediaStorageService>(provider => new MediaStorageService(
            provider.GetRequiredService<IDbContextFactory<MediaStorageDbContext>>(),
            provider.GetRequiredService<IWebHostEnvironment>().WebRootPath,
            provider.GetRequiredService<IMediaGitService>(),
            provider.GetRequiredService<TimeProvider>(),
            provider.GetRequiredService<MediaStorageOptions>().TrashRetention,
            provider.GetRequiredService<MediaStorageOptions>().MaxUploadBytes,
            provider.GetRequiredService<ILogger<MediaStorageService>>()
        ));
        builder.Services.AddHostedService<SoftDeletePurgeWorker>();

        // Лимиты приёма файлов. Дефолт Kestrel — 30 МБ на запрос, чего не
        // хватает: в хранилище лежат файлы по 63 МБ, и загрузка такого файла
        // падала бы с 413. Верхняя граница берётся из настроек, чтобы лимит
        // и проверка в MediaStorageService не расходились.
        var storageOptions = new MediaStorageOptions();
        builder.Configuration.GetSection(MediaStorageOptions.SectionName).Bind(storageOptions);

        var requestLimit = Math.Max(storageOptions.MaxUploadBytes * 2, 200L * 1024 * 1024);

        builder.Services.Configure<FormOptions>(
            options => options.MultipartBodyLengthLimit = requestLimit
        );

        // PyroAlerts services
        builder.Services.AddScoped<PyroAlertsHelper>();
        builder.Services.AddScoped<PyroAlertsHandler>();

        // RandomMeme service
        builder.Services.AddScoped<IRandomMemeService, RandomMemeService>();

        // Controllers
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMarsDefaults();

        // UI хранилища собирается Vite в ui-dist и раздаётся отсюда, а не из
        // wwwroot: wwwroot — git-версионируемый том, и статика внутри него
        // порождала бы коммиты и попадала бы в индекс хранилища как медиа.
        var uiRoot = Path.Combine(app.Environment.ContentRootPath, "ui-dist");

        if (Directory.Exists(uiRoot))
        {
            app.UseStaticFiles(
                new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(uiRoot),
                    RequestPath = "/storage-ui",
                    ServeUnknownFileTypes = false,
                }
            );
        }

        app.MapControllers();

        app.MapGet("/", () => "MARS.MediaStorage is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
