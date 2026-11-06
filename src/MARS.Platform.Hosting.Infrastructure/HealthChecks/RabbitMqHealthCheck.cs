using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Platform.Web.AspNetCore.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace MARS.Platform.Hosting.Infrastructure.HealthChecks;

/// <summary>
/// Проверка доступности брокера RabbitMQ: подключается с теми же учётными данными,
/// что и реальные продюсеры/потребители, и открывает канал.
/// Без этого healthcheck-а compose помечал сервис healthy при недоступном брокере,
/// хотя AMQP-обмен не работал.
/// </summary>
public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqOptions _options;
    private readonly Func<RabbitMqOptions, string, CancellationToken, Task<bool>> _probe;

    public RabbitMqHealthCheck(RabbitMqOptions options)
        : this(options, ProbeAsync) { }

    internal RabbitMqHealthCheck(
        RabbitMqOptions options,
        Func<RabbitMqOptions, string, CancellationToken, Task<bool>> probe
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(probe);

        _options = options;
        _probe = probe;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var isHealthy = await _probe(
                _options,
                $"mars-healthcheck-{context.Registration.Name}",
                cancellationToken
            );

            return isHealthy
                ? HealthCheckResult.Healthy("RabbitMQ доступен")
                : HealthCheckResult.Unhealthy("Соединение или канал RabbitMQ закрыты");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"RabbitMQ недоступен: {ex.Message}", ex);
        }
    }

    private static async Task<bool> ProbeAsync(
        RabbitMqOptions options,
        string clientProvidedName,
        CancellationToken cancellationToken
    )
    {
        var factory = RabbitMqConnectionFactory.Create(options, clientProvidedName);

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: cancellationToken
        );

        return connection.IsOpen && channel.IsOpen;
    }
}
