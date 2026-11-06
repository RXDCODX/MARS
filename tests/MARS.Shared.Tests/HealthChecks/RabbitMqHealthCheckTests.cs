using MARS.Shared.Configuration;
using MARS.Shared.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MARS.Shared.Tests.HealthChecks;

public sealed class RabbitMqHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsyncReturnsHealthyWhenBrokerIsAvailable()
    {
        var options = new RabbitMqOptions();
        var healthCheck = new RabbitMqHealthCheck(
            options,
            (actualOptions, clientName, _) =>
            {
                Assert.Same(options, actualOptions);
                Assert.Equal("mars-healthcheck-rabbitmq", clientName);
                return Task.FromResult(true);
            }
        );

        var result = await healthCheck.CheckHealthAsync(Context(healthCheck));

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("RabbitMQ доступен", result.Description);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsyncReturnsUnhealthyWhenBrokerIsClosed()
    {
        var healthCheck = new RabbitMqHealthCheck(
            new RabbitMqOptions(),
            (_, _, _) => Task.FromResult(false)
        );

        var result = await healthCheck.CheckHealthAsync(Context(healthCheck));

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Соединение или канал RabbitMQ закрыты", result.Description);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsyncReturnsUnhealthyWhenProbeThrows()
    {
        var exception = new InvalidOperationException("connection refused");
        var healthCheck = new RabbitMqHealthCheck(
            new RabbitMqOptions(),
            (_, _, _) => Task.FromException<bool>(exception)
        );

        var result = await healthCheck.CheckHealthAsync(Context(healthCheck));

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("RabbitMQ недоступен: connection refused", result.Description);
        Assert.Same(exception, result.Exception);
    }

    private static HealthCheckContext Context(IHealthCheck healthCheck) =>
        new()
        {
            Registration = new HealthCheckRegistration(
                "rabbitmq",
                healthCheck,
                HealthStatus.Unhealthy,
                Array.Empty<string>()
            ),
        };
}
