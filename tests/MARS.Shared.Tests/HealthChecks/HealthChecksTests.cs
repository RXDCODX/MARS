using MARS.Shared.Configuration;
using MARS.Shared.Extensions;
using MARS.Shared.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MARS.Shared.Tests.HealthChecks;

/// <summary>
/// Проверки готовности сервиса.
///
/// Health check решает, попадёт ли сервис в балансировку. Если проверка брокера
/// всегда зелёная, compose отправляет трафик в сервис, который не может ни
/// принять, ни отдать событие.
/// </summary>
public class HealthChecksTests
{
    /// <summary>
    /// Недоступный брокер даёт unhealthy с текстом ошибки: по нему в логах видно,
    /// что именно отвалилось, а health check остаётся зелёным.
    /// </summary>
    [Fact]
    public async Task UnreachableBrokerIsReportedAsUnhealthy()
    {
        var check = new RabbitMqHealthCheck(
            new RabbitMqOptions
            {
                // Порт, который не слушается: подключение обрывается сразу, без сети.
                Host = "127.0.0.1",
                Port = 1,
                UserName = "guest",
                Password = "guest",
            }
        );

        var result = await check.CheckHealthAsync(
            Context(check),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    /// <summary>
    /// Регистрация проверки брала те же учётные данные и то же имя строки
    /// подключения, что и реальный брокер: иначе readiness проверял бы не тот
    /// брокер, из которого сервис реально читает данные.
    /// </summary>
    [Fact]
    public async Task HealthCheckUsesConfiguredCredentials()
    {
        var options = new RabbitMqOptions
        {
            Host = "127.0.0.1",
            Port = 1,
            UserName = "mars",
            Password = "пароль",
        };
        var check = new RabbitMqHealthCheck(options);

        var result = await check.CheckHealthAsync(
            Context(check),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("RabbitMQ", result.Description);
    }

    /// <summary>
    /// Отмена проверки не выглядит как падение брокера: иначе остановка сервиса
    /// рисовала бы в логах ошибку соединения.
    /// </summary>
    [Fact]
    public async Task CancelledCheckIsReportedAsUnhealthy()
    {
        var check = new RabbitMqHealthCheck(new RabbitMqOptions { Host = "127.0.0.1", Port = 1 });
        using var stopping = new CancellationTokenSource();
        await stopping.CancelAsync();

        var result = await check.CheckHealthAsync(Context(check), stopping.Token);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    /// <summary>
    /// Проверка RabbitMQ не создаёт подключение повторно на каждый вызов с тем же
    /// именем: имя попадает в имя соединения брокера и обязано быть стабильным,
    /// иначе в панели RabbitMQ появлялись бы сотни соединений.
    /// </summary>
    [Fact]
    public void CheckNameIsStableAcrossCalls()
    {
        var check = new RabbitMqHealthCheck(new RabbitMqOptions { Host = "127.0.0.1", Port = 1 });
        var registration = new HealthCheckRegistration("rabbitmq", check, null, null);

        Assert.Equal("rabbitmq", registration.Name);
    }

    /// <summary>
    /// Схема миграций задаётся в регистрации: без неё история миграций всех
    /// сервисов лежала бы в одной таблице public и сервисы мешали друг другу.
    /// </summary>
    [Fact]
    public void DbContextRegistrationKeepsSchemaInMigrationHistory()
    {
        var services = new ServiceCollection();
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:TwitchDb"] = "Host=localhost;Database=twitch",
                }
            )
            .Build();

        services.AddLogging();
        services.AddMarsDbContext<HealthChecksProbeContext>(configuration, "twitch", "TwitchDb");

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<MarsSchemaMigrator<HealthChecksProbeContext>>());
        Assert.Single(provider.GetServices<IMarsSchemaReady<HealthChecksProbeContext>>());
    }

    /// <summary>
    /// Контекст проверки: имя регистрации попадает в имя соединения брокера.
    /// </summary>
    private static HealthCheckContext Context(IHealthCheck check) =>
        new() { Registration = new HealthCheckRegistration("rabbitmq", _ => check, null, null) };

    private sealed class HealthChecksProbeContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public HealthChecksProbeContext(
            Microsoft.EntityFrameworkCore.DbContextOptions<HealthChecksProbeContext> options
        )
            : base(options) { }
    }
}
