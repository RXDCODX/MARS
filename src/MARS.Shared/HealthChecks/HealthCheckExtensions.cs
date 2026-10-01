using MARS.Shared.Configuration;
using MARS.Shared.Extensions;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MARS.Shared.HealthChecks;

public static class HealthCheckExtensions
{
    /// <summary>
    /// Health checks сервиса.
    /// </summary>
    /// <param name="configuration">Конфигурация сервиса.</param>
    /// <param name="connectionName">
    /// Имя строки подключения проверяемой базы. Сервисы без своей базы
    /// (Gateway, Commands, Discord, OBS, TTS) передают null — тогда проверка
    /// postgresql не регистрируется вовсе, иначе compose-анкер раздал бы всем
    /// DefaultConnection и readiness «зелёной» без всякой базы.
    /// </param>
    public static IServiceCollection AddMarsHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration,
        string? connectionName
    )
    {
        var builder = services.AddHealthChecks();

        var connString = connectionName is null
            ? null
            : MarsConnectionStringResolver.Resolve(configuration, connectionName);

        if (!string.IsNullOrEmpty(connString))
        {
            builder.AddNpgSql(connString, name: "postgresql", tags: ["ready", "db"]);
        }

        var rabbitHost = configuration[$"{RabbitMqOptions.SectionName}:Host"];

        if (!string.IsNullOrEmpty(rabbitHost))
        {
            var rabbitOptions = RabbitMqConnectionFactory.CreateOptions(configuration);

            builder.Add(
                new HealthCheckRegistration(
                    "rabbitmq",
                    _ => new RabbitMqHealthCheck(rabbitOptions),
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["ready", "rabbitmq"]
                )
            );
        }

        return services;
    }
}
