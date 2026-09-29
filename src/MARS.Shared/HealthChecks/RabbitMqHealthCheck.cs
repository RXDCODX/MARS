using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace MARS.Shared.HealthChecks;

/// <summary>
/// Проверка доступности брокера RabbitMQ: подключается с теми же учётными данными,
/// что и реальные продюсеры/потребители, и открывает канал.
/// Без этого healthcheck-а compose помечал сервис healthy при недоступном брокере,
/// хотя AMQP-обмен не работал.
/// </summary>
public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqOptions _options;

    public RabbitMqHealthCheck(RabbitMqOptions options)
    {
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var factory = RabbitMqConnectionFactory.Create(
                _options,
                $"mars-healthcheck-{context.Registration.Name}"
            );

            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(
                cancellationToken: cancellationToken
            );

            var isHealthy = connection.IsOpen && channel.IsOpen;

            return isHealthy
                ? HealthCheckResult.Healthy("RabbitMQ доступен")
                : HealthCheckResult.Unhealthy("Соединение или канал RabbitMQ закрыты");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"RabbitMQ недоступен: {ex.Message}", ex);
        }
    }
}
