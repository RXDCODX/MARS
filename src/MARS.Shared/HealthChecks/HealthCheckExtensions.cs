using MARS.Shared.Configuration;
using MARS.Shared.Extensions;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MARS.Shared.HealthChecks;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddMarsHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var builder = services.AddHealthChecks();

        var connString = MarsConnectionStringResolver.Resolve(configuration);

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
