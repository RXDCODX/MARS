using MARS.Admin.CustomLoggers.TelegramLogger;
using MARS.Admin.Data;
using MARS.Admin.Services;
using MARS.Admin.Services.Configuration;
using MARS.Admin.Services.ServiceManager;
using MARS.Shared.Clients;
using MARS.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace MARS.Admin;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Admin", "AdminDb");

        // Database context.
        // Строка подключения обязательна: fallback на DefaultConnection маскировал бы
        // ошибку конфигурации, уводя сервис в чужую базу вместо явной ошибки.
        var adminConnectionString =
            builder.Configuration.GetConnectionString("AdminDb")
            ?? throw new InvalidOperationException(
                "Не задана строка подключения ConnectionStrings:AdminDb"
            );

        builder.Services.AddDbContextFactory<AdminDbContext>(options =>
        {
            options.UseNpgsql(
                adminConnectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "admin")
            );
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        // AddDbContextFactory регистрирует IDbContextFactory, а
        // AddMarsSchemaMigration его и требует. Порядок важен: фабрика должна
        // быть в контейнере до того, как мигратор её запросит.
        builder.Services.AddMarsSchemaMigration<AdminDbContext>();

        // HTTP client for health checks
        builder.Services.AddHttpClient("HealthCheck");

        // Services
        builder.Services.AddSingleton<IServiceManager, ServiceManager>();
        builder.Services.AddHostedService<ConfigurationKeysBootstrapHostedService>();

        // Контроллеры admin-API. Без AddControllers() вызов app.MapControllers()
        // на старте падает с InvalidOperationException «Unable to find the required
        // services», и сервис уходит в restart-loop.
        // Shikimori-мониторинг: клиент и рейт-лимитер живут в MARS.Shikimori,
        // в Admin остался только эндпоинт для панели.
        builder.Services.AddMarsServiceClients(builder.Configuration);
        builder.Services.AddMarsServiceClient<IShikimoriApiClient, ShikimoriApiClient>(
            ServiceClientExtensions.ShikimoriHttpClientName
        );
        builder.Services.AddScoped<IShikimoriRateLimiterService, ShikimoriRateLimiterService>();

        // Контроллеры admin-API. Без AddControllers() вызов app.MapControllers()

        // Custom loggers
        builder.Logging.AddTelegramLogger(options =>
        {
            options.BotToken = builder.Configuration["Telegram:BotToken"] ?? "";
            options.ChatId =
                builder.Configuration.GetSection("Telegram:LogChatIds").Get<long[]>() ?? [];
            options.SourceName = "MARS.Admin";
            options.MinimumLevel = LogLevel.Warning;
        });

        var app = builder.Build();

        app.UseMarsDefaults();

        // Blocker №6 (аудит): админские эндпоинты меняют состояние процесса
        // (Environment.SetEnvironmentVariable), читают всю таблицу root_state
        // и проверяют произвольные пути ФС. Схема ServiceApiKey fail-closed:
        // без ServiceAuth:ApiKey в конфигурации политика не пройдёт и хаб
        // не откроется ни для кого.
        var serviceApiKey = builder.Configuration["ServiceAuth:ApiKey"];

        if (string.IsNullOrWhiteSpace(serviceApiKey))
        {
            throw new InvalidOperationException(
                "ServiceAuth:ApiKey не задан. Админские API защищены обязательной "
                    + "аутентификацией по ключу; задайте ServiceAuth__ApiKey в окружении."
            );
        }

        app.MapControllers();

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
