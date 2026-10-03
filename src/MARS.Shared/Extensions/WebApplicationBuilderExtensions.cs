using System.Text.Json.Serialization;
using MARS.Shared.Configuration;
using MARS.Shared.HealthChecks;
using MARS.Shared.Logging;
using MARS.Shared.Messaging;
using MARS.Shared.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace MARS.Shared.Extensions;

public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Стандартная настройка для всех MARS сервисов.
    /// Включает: Serilog, OpenTelemetry, Health Checks, CORS, Swagger, RabbitMQ
    /// </summary>
    /// <param name="serviceName">Имя сервиса для логов, телеметрии и Swagger.</param>
    /// <param name="dbConnectionName">
    /// Имя строки подключения собственной базы сервиса. null у сервисов без базы
    /// (Gateway, Commands, Discord, OBS, TTS) — тогда проверка postgresql не
    /// регистрируется. Значение обязано совпадать с тем, которое передаётся в
    /// AddMarsDbContext: расхождение даёт «зелёный» readiness при недоступной
    /// базе, из которой сервис читает данные.
    /// </param>
    public static WebApplicationBuilder AddMarsDefaults(
        this WebApplicationBuilder builder,
        string serviceName,
        string? dbConnectionName = null
    )
    {
        // Logging
        builder.Host.UseSerilog((ctx, lc) => lc.AddMarsLogging(ctx.Configuration, serviceName));

        // Telemetry
        builder.Services.AddMarsTelemetry(builder.Configuration, serviceName);

        // Health Checks
        builder.Services.AddMarsHealthChecks(builder.Configuration, dbConnectionName);

        // RabbitMQ (если настроен)
        var rabbitOptions = RabbitMqConnectionFactory.CreateOptions(builder.Configuration);
        if (!string.IsNullOrEmpty(rabbitOptions.Host))
        {
            builder.Services.AddSingleton(rabbitOptions);

            // Потребители принимают IOptions<RabbitMqOptions>. Без этой привязки
            // IOptions отдавал бы новый пустой экземпляр с дефолтами
            // (localhost/mars/mars), и в Docker каждый консьюмер бесконечно
            // переподключался бы к localhost вместо брокера из compose.
            builder
                .Services.AddOptions<RabbitMqOptions>()
                .Configure<RabbitMqOptions>(
                    (options, configured) =>
                    {
                        options.Host = configured.Host;
                        options.Port = configured.Port;
                        options.UserName = configured.UserName;
                        options.Password = configured.Password;
                        options.PasswordFile = configured.PasswordFile;
                        options.VirtualHost = configured.VirtualHost;
                        options.ReconnectDelayMilliseconds = configured.ReconnectDelayMilliseconds;
                        options.MaxDeliveryAttempts = configured.MaxDeliveryAttempts;
                    }
                );

            builder.Services.AddSingleton<IMarsEventBus>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<RabbitMqEventBus>>();
                return new RabbitMqEventBus(rabbitOptions, serviceName, logger);
            });
        }

        // Аутентификация и авторизация защищают внутренние REST-эндпоинты сервисов.
        builder.Services.AddMarsAuthentication(builder.Configuration);

        // CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(
                "CorsPolicy",
                policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
            );
        });

        // Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(
                "v1",
                new Microsoft.OpenApi.Models.OpenApiInfo { Title = serviceName, Version = "v1" }
            );
        });

        // Configuration binding
        builder.Services.Configure<ServiceEndpoints>(
            builder.Configuration.GetSection(ServiceEndpoints.SectionName)
        );

        // Перечисления сериализуются именами, а не числами.
        //
        // Клиент сгенерирован из контракта, где все перечисления — строковые:
        // `PlayerStateStateEnum.Playing === "Playing"`, а не 1. Без этого
        // System.Text.Json отдаёт число, и на браузере сравнение
        // `state === PlayerStateStateEnum.Playing` никогда не сходится: плеер не
        // видел бы состояния «играет», а страница команд падала бы на
        // `availablePlatforms.length`, потому что приходит `[4, 2]`, а не имена.
        //
        // Настройка именно здесь, а не в каждом Program.cs: `AddMarsDefaults`
        // зовут все сервисы, и правило, о котором забывают в одном из них,
        // расходится ровно так же, как расходился бы забытый `AddControllers`.
        //
        // Через `Configure<JsonOptions>`, а не через `AddControllers()`: у
        // MARS.Videos365 контроллеров нет вовсе, и добавлять MVC в общих
        // настройках значило бы менять состав сервиса, а не формат ответа.
        //
        // Межсервисный HTTP настроен так же — в ServiceHttpClientBase; здесь речь
        // о JSON, который видит браузер.
        builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return builder;
    }
}
