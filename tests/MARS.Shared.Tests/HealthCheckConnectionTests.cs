using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MARS.Shared.HealthChecks;

namespace MARS.Shared.Tests;

/// <summary>
/// Проверки подключения health check к базе сервиса.
/// </summary>
/// <remarks>
/// Проверка postgresql обязана смотреть в ту же базу, из которой сервис читает
/// данные. До разделения баз она всегда брала DefaultConnection, и все сервисы
/// делили одну базу с общим якорем в compose. Теперь имя строки передаётся
/// явно, а сервисы без базы проверку не регистрируют вовсе.
/// Тест проверяет наблюдаемое поведение (какие проверки реально выполняются), а
/// не внутренние коллекции: HealthCheckServiceOptions наружу не отдаётся, и
/// утверждения о структуре регистраций ничего не говорили бы о работе сервиса.
/// </remarks>
public class HealthCheckConnectionTests
{
    private static IConfiguration Config(params (string Name, string Value)[] connections)
    {
        var values = connections.ToDictionary(c => $"ConnectionStrings:{c.Name}", c => c.Value);

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static async Task<HealthReport> RunChecksAsync(
        IConfiguration configuration,
        string? connectionName
    )
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddMarsHealthChecks(configuration, connectionName);

        var provider = services.BuildServiceProvider();

        var report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync(
            TestContext.Current.CancellationToken
        );

        return report;
    }

    [Fact]
    public async Task PostgresCheckIsExecuted_WhenConnectionNameResolves()
    {
        var configuration = Config(
            ("TwitchDb", "Host=localhost;Database=mars_twitch;Username=mars_twitch;Password=x")
        );

        var report = await RunChecksAsync(configuration, "TwitchDb");

        Assert.Contains(report.Entries, e => e.Key == "postgresql");
    }

    /// <summary>
    /// Сервисы без своей базы (Gateway, Commands, Discord, OBS, TTS) не должны
    /// выполнять проверку postgresql: иначе compose-доступность считалась бы
    /// зелёной по чужой базе.
    /// </summary>
    [Fact]
    public async Task PostgresCheckIsAbsent_WhenServiceHasNoDatabase()
    {
        var configuration = Config(("TwitchDb", "Host=localhost;Database=mars_twitch"));

        var report = await RunChecksAsync(configuration, connectionName: null);

        Assert.DoesNotContain(report.Entries, e => e.Key == "postgresql");
    }

    /// <summary>
    /// Имя строки передаётся явно, а не по умолчанию DefaultConnection: с
    /// умолчанием сервис проверял бы чужую базу и получал бы зелёный readiness
    /// при недоступной своей.
    /// </summary>
    [Fact]
    public async Task PostgresCheckIsAbsent_WhenNamedConnectionIsMissing()
    {
        var configuration = Config(("DefaultConnection", "Host=localhost;Database=mars"));

        var report = await RunChecksAsync(configuration, "MediaStorageDb");

        Assert.DoesNotContain(report.Entries, e => e.Key == "postgresql");
    }

    [Fact]
    public async Task PostgresCheckIsAbsent_WhenNamedConnectionIsEmpty()
    {
        var configuration = Config(("MediaStorageDb", string.Empty));

        var report = await RunChecksAsync(configuration, "MediaStorageDb");

        Assert.DoesNotContain(report.Entries, e => e.Key == "postgresql");
    }

    /// <summary>
    /// Readiness обязан краснеть, когда своя база недоступна: иначе
    /// docker-compose считал бы сервис готовым и не перезапускал его.
    /// </summary>
    [Fact]
    public async Task ReportIsUnhealthy_WhenOwnDatabaseIsUnreachable()
    {
        var configuration = Config(
            (
                "Videos365Db",
                "Host=127.0.0.1;Port=1;Database=mars_videos365;Username=x;Password=x;Timeout=2"
            )
        );

        var report = await RunChecksAsync(configuration, "Videos365Db");

        Assert.Equal(HealthStatus.Unhealthy, report.Status);
    }
}
