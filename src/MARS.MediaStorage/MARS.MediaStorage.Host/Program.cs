using MARS.MediaStorage.Application.Alerts;
using MARS.MediaStorage.Application.Git;
using MARS.MediaStorage.Application.Media;
using MARS.MediaStorage.Application.RandomMemes;
using MARS.MediaStorage.Application.Storage;
using MARS.MediaStorage.Host.Hubs;
using MARS.MediaStorage.Host.Services;
using MARS.MediaStorage.Infrastructure.Git;
using MARS.MediaStorage.Infrastructure.Media;
using MARS.MediaStorage.Infrastructure.Postgres.Alerts;
using MARS.MediaStorage.Infrastructure.Postgres.Data;
using MARS.MediaStorage.Infrastructure.Postgres.RandomMemes;
using MARS.MediaStorage.Infrastructure.Postgres.Storage;
using MARS.MediaStorage.Infrastructure.Telegram;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Platform.Infrastructure.Persistence.Postgres.Extensions;
using MARS.Platform.Infrastructure.Web.AspNetCore.Extensions;
using MARS.Shared.Contracts.Telegram;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace MARS.MediaStorage.Host;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.MediaStorage", "MediaStorageDb");
        builder.Services.AddMarsSignalR();

        // Проверяем корень wwwroot до запуска любых фоновых обработчиков: при
        // неверном bind mount приложение должно завершиться, не трогая файлы.
        builder.Services.AddSingleton<MediaGitInitializer>();

        // DbContext
        builder.Services.AddMarsDbContext<MediaStorageDbContext>(
            builder.Configuration,
            "mediastorage",
            "MediaStorageDb"
        );

        // Media services
        builder.Services.AddScoped<IMediaFileStorageService, WebRootMediaFileStorageService>();

        // Инспектор, transcoder и подготовка мема уходят в фон MemeMediaTranscodeWorker,
        // то есть в синглтон, поэтому и сами они — синглтоны. Все три stateless:
        // полей нет, работа с файлами идемпотентна через кэш. С scoped здесь
        // ValidateScopes роняет контейнер в Development.
        builder.Services.AddSingleton<IMediaInspector, FfprobeMediaInspector>();
        builder.Services.AddSingleton<IMediaTranscoder, MediaTranscoder>();

        // Отчёт о перекодировании уходит в Telegram. Клиент регистрируется
        // только когда задан токен, поэтому воркер получает nullable-мессенджер:
        // без Telegram перекодирование всё равно работает.
        builder.Services.AddMarsTelegramOptions(builder.Configuration);

        var telegramBotToken = builder.Configuration.ResolveTelegramBotToken();

        if (!string.IsNullOrEmpty(telegramBotToken))
        {
            builder.Services.AddSingleton<ITelegramBotClient>(
                new TelegramBotClient(telegramBotToken)
            );
            builder.Services.AddSingleton<ITelegramAdminMessenger, TelegramAdminMessenger>();
        }
        else
        {
            // Без токена мессенджер тоже должен быть зарегистрирован: иначе
            // ActivatorUtilities не сможет собрать MemeMediaTranscodeWorker и хост
            // упадёт на старте. Nullable-аннотация в конструкторе для контейнера
            // не значит ничего.
            builder.Services.AddSingleton<ITelegramAdminMessenger, NullTelegramAdminMessenger>();
        }

        // Перекодирование мемов (AD13). Живёт здесь, а не в MARS.TwitchCore:
        // таблицы MemeOrder и Alerts принадлежат хранилищу, и ffmpeg тоже его.
        builder.Services.AddSingleton<MemeMediaPreparationService>();
        builder.Services.AddHostedService<MemeMediaTranscodeWorker>();

        // Git-синхронизация wwwroot. Выключена по умолчанию: при Enabled=false
        // никакой commit/push не выполняется. Рабочая копия — WebRootPath,
        // метаданные Git живут рядом с содержимым в обязательном host bind mount.
        builder.Services.Configure<MediaGitOptions>(
            builder.Configuration.GetSection(MediaGitOptions.SectionName)
        );
        // IOptions<T> сам по себе не регистрирует и сам T, а MediaGitInitializer
        // принимает MediaGitOptions напрямую.
        builder.Services.AddSingleton(provider =>
            provider.GetRequiredService<IOptions<MediaGitOptions>>().Value
        );
        builder.Services.AddSingleton<IMediaGitService>(provider => new MediaGitService(
            provider.GetRequiredService<IOptions<MediaGitOptions>>().Value,
            provider.GetRequiredService<IWebHostEnvironment>().WebRootPath
        ));
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
        builder.Services.AddScoped<
            IMediaStorageBrowserService,
            FileSystemMediaStorageBrowserService
        >();
        builder.Services.AddHostedService<SoftDeletePurgeWorker>();
        builder.Services.AddHostedService<MediaStorageFileWatcher>();

        // Лимиты приёма файлов. Дефолт Kestrel — 30 МБ на запрос, чего не
        // хватает: в хранилище лежат файлы по 63 МБ, и загрузка такого файла
        // падала бы с 413. Верхняя граница берётся из настроек, чтобы лимит
        // и проверка в MediaStorageService не расходились.
        var storageOptions = new MediaStorageOptions();
        builder.Configuration.GetSection(MediaStorageOptions.SectionName).Bind(storageOptions);

        var requestLimit = Math.Max(storageOptions.MaxUploadBytes * 2, 200L * 1024 * 1024);

        builder.Services.Configure<FormOptions>(options =>
            options.MultipartBodyLengthLimit = requestLimit
        );

        // PyroAlerts services.
        //
        // Регистраций здесь нет намеренно. PyroAlertsHelper и PyroAlertsHandler
        // нигде не потребляются: рабочий близнец этого конвейера живёт в
        // MARS.Alerts и ходит в оверлей через ITelegramusNotifier, а локальный
        // IAlertNotifier не имеет ни одной реализации во всём репозитории.
        // Пока он был зарегистрирован, контейнер в Development падал на
        // «Unable to resolve service for type IAlertNotifier», а в Production
        // упал бы в первый же вызов — если бы он вообще случился. Разрыв
        // оставлен намеренно: удалить тройку или довести её до gRPC-пути —
        // решение владельца, и оно уже не про то, чтобы стенд поднимался.

        // RandomMeme service. Сервис и подготовка stateless: контекст создаёт
        // IDbContextFactory (синглтон) на каждый вызов, состояния нет. Оба
        // потребляет MemeMediaTranscodeWorker — фоновая служба, то есть
        // синглтон, и scoped здесь означал бы падение ValidateScopes.
        builder.Services.AddSingleton<IRandomMemeService, RandomMemeService>();

        // Алерты: контроллер работает только с HTTP, сценарии живут здесь.
        builder.Services.AddScoped<IAlertService, AlertService>();

        // Controllers
        builder.Services.AddControllers();

        var app = builder.Build();

        // Проверка обязательного host repository выполняется до миграций БД:
        // при неверном bind mount сервис должен завершиться, ничего не меняя.
        app.Services.GetRequiredService<MediaGitInitializer>()
            .StartAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        app.UseMarsDefaults();

        // Медиа из wwwroot.
        //
        // wwwroot — обязательный общий bind mount MEDIA_STORAGE_HOST_PATH, куда кладутся файлы оверлея: ролики
        // алертов и звуки в Alerts/. Клиент ссылается на них прямо из разметки
        // (`/Alerts/bell.wav`, `/Alerts/svadba.mp3`, `/Alerts/mute.png`), и без
        // раздачи эти запросы уходили в SPA-заглушку и отдавали index.html:
        // браузер получал 200 с text/html и тихо не играл звук.
        //
        // Маршрут в Gateway при этом узкий — `/Alerts/{**remainder}`, — поэтому
        // наружу уходит только папка Alerts, а не весь том целиком.
        //
        // Раздача включается всегда, в том числе на пустом томе: каталога может
        // не быть, и тогда ни один файл не отдаётся — это нормальное состояние
        // свежего развёртывания, а не ошибка.
        app.UseStaticFiles();

        // UI хранилища собирается Vite в ui-dist и раздаётся отсюда, а не из
        // wwwroot: wwwroot — git-версионируемый том, и статика внутри него
        // порождала бы коммиты и попадала бы в индекс хранилища как медиа.
        // В production ui-dist лежит рядом с опубликованным приложением.
        // Dev-контейнер держит те же файлы вне /src: bind mount исходников
        // перекрывает содержимое /src и не должен скрывать собранную статику.
        var configuredUiRoot = builder.Configuration["StorageUi:Root"];
        var uiRoot = string.IsNullOrWhiteSpace(configuredUiRoot)
            ? Path.Combine(app.Environment.ContentRootPath, "ui-dist")
            : Path.GetFullPath(configuredUiRoot);

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
            app.Logger.LogInformation("Storage UI is served from {UiRoot}", uiRoot);
        }
        else
        {
            app.Logger.LogWarning(
                "Storage UI assets were not found at {UiRoot}; /storage-ui will return 404",
                uiRoot
            );
        }

        app.MapHub<MediaStorageHub>("/hubs/media-storage");
        app.MapControllers();

        app.MapGet("/", () => "MARS.MediaStorage is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
