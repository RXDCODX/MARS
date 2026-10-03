using MARS.Shared.Clients;
using MARS.Shared.Configuration;
using MARS.Shared.Extensions;
using MARS.TestKit.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Tests.Extensions;

/// <summary>
/// Регистрация общей инфраструктуры: клиентов, контекстов БД и прогона миграций.
///
/// Проверяется, что зависимости действительно резолвятся и что имена совпадают с
/// теми, что использует код: опечатка в имени строки подключения или клиента дала
/// бы зелёную сборку и пустую базу уже в рантайме.
/// </summary>
public class ServiceRegistrationTests
{
    [Fact]
    public void ServiceClientsAreRegisteredWithAddresses()
    {
        var factory = BuildClientFactory(new Dictionary<string, string?>());

        Assert.Equal(
            "http://media-storage:8080/",
            Address(factory, ServiceClientExtensions.MediaStorageHttpClientName)
        );
        Assert.Equal(
            "http://waifu-gacha:8080/",
            Address(factory, ServiceClientExtensions.WaifuGachaHttpClientName)
        );
        Assert.Equal(
            "http://twitch-core:8080/",
            Address(factory, ServiceClientExtensions.LeaderboardHttpClientName)
        );
        Assert.Equal(
            "http://discord:8080/",
            Address(factory, ServiceClientExtensions.DiscordHttpClientName)
        );
        Assert.Equal(
            "http://shikimori:8080/",
            Address(factory, ServiceClientExtensions.ShikimoriHttpClientName)
        );
    }

    [Fact]
    public void ServiceClientsUseConfiguredAddresses()
    {
        var factory = BuildClientFactory(
            new Dictionary<string, string?>
            {
                ["ServiceEndpoints:MediaStorage"] = "http://storage:9090",
            }
        );

        Assert.Equal(
            "http://storage:9090/",
            Address(factory, ServiceClientExtensions.MediaStorageHttpClientName)
        );
    }

    [Fact]
    public void TypedServiceClientIsRegisteredByName()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<ServiceEndpoints>();
        services.AddMarsServiceClient<IDiscordClient, DiscordClient>(
            ServiceClientExtensions.DiscordHttpClientName
        );

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IDiscordClient>();

        Assert.IsType<DiscordClient>(client);
        Assert.Equal("http://discord:8080", ((DiscordClient)client).ServiceEndpoint);
    }

    [Fact]
    public void SchemaMigrationIsRegisteredWithMigrator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var factory = new SchemaPostgresFactory();
        services.AddSingleton<IDbContextFactory<SchemaTestContext>>(factory);
        services.AddMarsSchemaMigration<SchemaTestContext>();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<MarsSchemaMigrator<SchemaTestContext>>());
        Assert.NotNull(provider.GetRequiredService<IMarsSchemaReady<SchemaTestContext>>());
        Assert.NotNull(provider.GetRequiredService<MarsSchemaStartupMarker>());
        factory.Dispose();
    }

    /// <summary>
    /// Строка подключения обязана быть найдена по имени: значение по умолчанию
    /// увело бы сервис в чужую базу, а readiness проверял бы не ту.
    /// </summary>
    [Fact]
    public void DbContextIsRegisteredForNamedConnection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMarsDbContext<SchemaTestContext>(
            Configuration(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Schema"] =
                        "Host=localhost;Database=mars;Username=u;Password=p",
                }
            ),
            schema: "schema_test",
            connectionName: "Schema",
            applyMigrations: false
        );

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IDbContextFactory<SchemaTestContext>>());
    }

    [Fact]
    public void DbContextWithoutMigrationsDoesNotRegisterMigrator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMarsDbContext<SchemaTestContext>(
            Configuration(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Schema"] =
                        "Host=localhost;Database=mars;Username=u;Password=p",
                }
            ),
            schema: "schema_test",
            connectionName: "Schema",
            applyMigrations: false
        );

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(MarsSchemaStartupMarker)
        );
    }

    [Fact]
    public async Task StartupMarkerKeepsSchemaNameAndRunsMigration()
    {
        ILogger? captured = null;
        var called = false;
        var marker = new MarsSchemaStartupMarker(
            "admin",
            logger => captured = logger,
            _ =>
            {
                called = true;
                return Task.CompletedTask;
            }
        );

        marker.SetLogger(NullLogger.Instance);
        await marker.MigrateAsync(TestContext.Current.CancellationToken);

        Assert.Equal("admin", marker.SchemaName);
        Assert.NotNull(captured);
        Assert.True(called);
    }

    /// <summary>
    /// Миграции применяются один раз: синхронный прогон до <c>app.Run()</c> и
    /// фоновый сервис не должны удваивать работу.
    /// </summary>
    [Fact]
    public async Task MigratorIsIdempotentForRepeatedCallers()
    {
        var factory = new SchemaPostgresFactory();
        var migrator = new MarsSchemaMigrator<SchemaTestContext>(factory);

        await migrator.MigrateAsync(TestContext.Current.CancellationToken);
        await migrator.MigrateAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, factory.Calls);
    }

    [Fact]
    public async Task SchemaReadySignalDelegatesToMigrator()
    {
        var factory = new SchemaPostgresFactory();
        var signal = new MarsSchemaReadySignal<SchemaTestContext>(
            new MarsSchemaMigrator<SchemaTestContext>(factory)
        );

        await signal.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, factory.Calls);
    }

    [Fact]
    public async Task MigratorKeepsProvidedLogger()
    {
        var logger = new CapturingLogger();
        var migrator = new MarsSchemaMigrator<SchemaTestContext>(new SchemaPostgresFactory())
        {
            Logger = logger,
        };

        await migrator.MigrateAsync(TestContext.Current.CancellationToken);

        Assert.Same(logger, migrator.Logger);
        Assert.NotEmpty(logger.Messages);
    }

    /// <summary>
    /// Строка подключения читается по имени, а неизвестное имя даёт пусто:
    /// это проверяется вызывающим (AddMarsDbContext падает на UseNpgsql с null).
    /// </summary>
    [Fact]
    public void ConnectionStringResolverReadsByName()
    {
        var configuration = Configuration(
            new Dictionary<string, string?> { ["ConnectionStrings:Known"] = "Host=localhost" }
        );

        Assert.Equal(
            "Host=localhost",
            MarsConnectionStringResolver.Resolve(configuration, "Known")
        );
        Assert.Null(MarsConnectionStringResolver.Resolve(configuration, "Unknown"));
    }

    /// <summary>
    /// Секрет из docker-compose подставляется содержимым файла: строка подключения
    /// в конфигурации хранит только путь, сам пароль туда не попадает.
    /// </summary>
    [Fact]
    public void PasswordFileIsReplacedWithSecretContent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mars-secret-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, "пароль-из-секрета");

        try
        {
            var resolved = MarsConnectionStringResolver
                .ResolvePasswordFile($"Host=localhost;Password_FILE={path};Database=mars")
                .Replace(Path.GetTempPath(), Path.GetTempPath());

            Assert.Contains("Password=\"пароль-из-секрета\"", resolved);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MissingPasswordFileIsReported()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mars-missing-{Guid.NewGuid():N}.txt");

        Assert.Throws<FileNotFoundException>(() =>
            MarsConnectionStringResolver.ResolvePasswordFile($"Host=localhost;Password_FILE={path}")
        );
    }

    [Fact]
    public void ConnectionStringWithoutSecretIsUnchanged()
    {
        Assert.Equal(
            "Host=localhost;Password=обычный",
            MarsConnectionStringResolver.ResolvePasswordFile("Host=localhost;Password=обычный")
        );
    }

    [Fact]
    public void EndpointLookupRejectsUnknownService()
    {
        var options = Options.Create(new ServiceEndpoints());

        Assert.Equal(options.Value.MediaStorage, options.GetServiceEndpoint("MediaStorage"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            options.GetServiceEndpoint("NoSuchService")
        );
    }

    private static IHttpClientFactory BuildClientFactory(Dictionary<string, string?> values)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMarsServiceClients(Configuration(values));

        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
    }

    private static string Address(IHttpClientFactory factory, string name) =>
        factory.CreateClient(name).BaseAddress!.ToString();

    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    /// <summary>
    /// Пробный контекст поверх живой PostgreSQL: он без миграций, поэтому схема
    /// строится <c>EnsureCreated</c>. Проверяется сам путь мигратора, а не
    /// содержимое чужих миграций.
    /// </summary>
    private sealed class SchemaPostgresFactory : PostgresTestDbContextFactory<SchemaTestContext>
    {
        public int Calls { get; private set; }

        public override Task<SchemaTestContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default
        )
        {
            Calls++;

            return base.CreateDbContextAsync(cancellationToken);
        }
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => Messages.Add(formatter(state, exception));
    }
}

internal sealed class SchemaTestContext(DbContextOptions<SchemaTestContext> options)
    : DbContext(options)
{
    public DbSet<SchemaTestEntity> Entities => Set<SchemaTestEntity>();
}

internal sealed class SchemaTestEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
