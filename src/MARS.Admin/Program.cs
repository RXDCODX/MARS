using MARS.Admin.CustomLoggers.DatabaseLogger;
using MARS.Admin.CustomLoggers.SignalRLogger;
using MARS.Admin.CustomLoggers.TelegramLogger;
using MARS.Admin.Data;
using MARS.Admin.Hubs;
using MARS.Admin.Services.Configuration;
using MARS.Admin.Services.Logs;
using MARS.Admin.Services.ServiceManager;
using MARS.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace MARS.Admin;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Admin");

        // Database contexts
        var adminConnectionString =
            builder.Configuration.GetConnectionString("AdminDb")
            ?? builder.Configuration.GetConnectionString("DefaultConnection");
        var logsConnectionString =
            builder.Configuration.GetConnectionString("LogsDb") ?? adminConnectionString;

        builder.Services.AddDbContextFactory<AdminDbContext>(options =>
        {
            options.UseNpgsql(
                adminConnectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "admin")
            );
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        builder.Services.AddDbContextFactory<LoggerDbContext>(options =>
        {
            options.UseNpgsql(
                logsConnectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "logs")
            );
        });

        // Миграции схемы admin применяются на старте (LoggerDbContext мигрирует сам)
        builder.Services.AddMarsSchemaMigration<AdminDbContext>();

        // HTTP client for health checks
        builder.Services.AddHttpClient("HealthCheck");

        // Services
        builder.Services.AddScoped<ILogsService, LogsService>();
        builder.Services.AddSingleton<IServiceManager, ServiceManager>();
        builder.Services.AddHostedService<ConfigurationKeysBootstrapHostedService>();

        // SignalR
        builder.Services.AddSignalR();

        // Контроллеры admin-API. Без AddControllers() вызов app.MapControllers()
        // на старте падает с InvalidOperationException «Unable to find the required
        // services», и сервис уходит в restart-loop.
        builder.Services.AddControllers();

        // Custom loggers
        builder.Logging.AddTelegramLogger(options =>
        {
            options.BotToken = builder.Configuration["Telegram:BotToken"] ?? "";
            options.ChatId =
                builder.Configuration.GetSection("Telegram:LogChatIds").Get<long[]>() ?? [];
            options.SourceName = "MARS.Admin";
            options.MinimumLevel = LogLevel.Warning;
        });

        builder.Logging.AddSignalRLogger(options =>
        {
            options.SourceName = "MARS.Admin";
            options.MinimumLogLevel = LogLevel.Information;
        });

        builder.Logging.AddDbLogger(
            () =>
                new DbLoggerOptions
                {
                    Factory = new LoggerDbContextFactory(options =>
                        options.UseNpgsql(logsConnectionString)
                    ),
                    MinimumLogLevel = LogLevel.Information,
                    Environment = builder.Environment,
                }
        );

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

        // Set SignalR hub context for the logger
        using (var scope = app.Services.CreateScope())
        {
            var hubContext =
                scope.ServiceProvider.GetService<Microsoft.AspNetCore.SignalR.IHubContext<
                    LoggerHub,
                    MARS.Admin.Hubs.Interfaces.ILoggerHub
                >>();
            if (hubContext is not null)
            {
                SignalRLogger.HubContext = hubContext;
            }
        }

        app.MapHub<LoggerHub>("/hubs/logger").RequireAuthorization();
        app.MapControllers();

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
