using System;
using MARS.Admin.Application.Configuration;
using MARS.Admin.Application.Services;
using MARS.Admin.Application.Services.ServiceManager;
using MARS.Admin.Infrastructure.Integrations.Shikimori.DependencyInjection;
using MARS.Admin.Infrastructure.Persistence.Postgres.Data;
using MARS.Admin.Infrastructure.Persistence.Postgres.DependencyInjection;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Platform.Infrastructure.Persistence.Postgres.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Admin.Api;

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

        builder.Services.AddAdminPersistence(adminConnectionString);
        builder.Services.Configure<ServiceEndpoints>(
            builder.Configuration.GetSection("ServiceEndpoints")
        );

        // HTTP client for health checks
        builder.Services.AddHttpClient("HealthCheck");

        // Services
        builder.Services.AddSingleton<IServiceManager, ServiceManager>();

        // Контроллеры admin-API. Без AddControllers() вызов app.MapControllers()
        // на старте падает с InvalidOperationException «Unable to find the required
        // services», и сервис уходит в restart-loop.
        // Shikimori-мониторинг: клиент и рейт-лимитер живут в MARS.Shikimori,
        // в Admin остался только эндпоинт для панели.
        builder.Services.AddAdminShikimoriIntegration(builder.Configuration);

        // Контроллеры admin-API. Без AddControllers() вызов app.MapControllers()
        // падает на старте с «Unable to find the required services», а admin стоит
        // в depends_on gateway — не поднимался бы весь стенд.
        builder.Services.AddControllers();

        // Журнал в Telegram больше не настраивается здесь: сборщик записей живёт
        // в MARS.Shared и подключается в AddMarsDefaults для всех сервисов сразу,
        // а доставляет MARS.Telegram — единственный владелец бота. Логгер из
        // builder.Logging не работал вовсе: AddMarsDefaults подменяет
        // ILoggerFactory на SerilogLoggerFactory, и провайдер оттуда не
        // вызывался ни разу.

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
