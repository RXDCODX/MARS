using MARS.Shared.Development;
using Microsoft.Extensions.Configuration;

namespace MARS.Shared.Tests.Development;

/// <summary>
/// Разбор адресов инфраструктуры из конфигурации сервиса.
/// </summary>
/// <remarks>
/// Проверяются строки, которые реально лежат в <c>appsettings.json</c> и в профиле
/// MARS.Local: своя строка подключения, адрес брокера и адрес OTLP. Ошибка здесь
/// не роняет запуск, а уводит автостарт в сторону: «целей нет» означает «нечем
/// проверить», и тогда поднимается всё подряд.
/// </remarks>
public class DevInfrastructureTargetResolverTests
{
    private static IConfiguration Config(params (string Name, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                values.ToDictionary(value => value.Name, value => (string?)value.Value)
            )
            .Build();

    [Fact]
    public void Строка_подключения_даёт_цель_postgres()
    {
        var targets = DevInfrastructureTargetResolver.Resolve(
            Config(
                (
                    "ConnectionStrings:TwitchDb",
                    "Host=localhost;Port=6017;Database=mars_twitch;Username=mars;Password=secret"
                )
            )
        );

        var target = Assert.Single(targets);
        Assert.Equal("postgres", target.Service);
        Assert.Equal("localhost", target.Host);
        Assert.Equal(6017, target.Port);
        Assert.Equal("ConnectionStrings:TwitchDb", target.Source);
        Assert.Equal("localhost:6017", target.Address);
        Assert.True(target.IsLocal);
    }

    [Fact]
    public void Без_порта_берётся_порт_по_умолчанию()
    {
        var targets = DevInfrastructureTargetResolver.Resolve(
            Config(("ConnectionStrings:MediaDb", "Server=127.0.0.1;Database=mars_media"))
        );

        var target = Assert.Single(targets);
        Assert.Equal(5432, target.Port);
        Assert.True(target.IsLocal);
    }

    [Fact]
    public void Нечисловой_порт_не_роняет_разбор()
    {
        var targets = DevInfrastructureTargetResolver.Resolve(
            Config(("ConnectionStrings:MediaDb", "Host=localhost;Port=шесть;Database=mars_media"))
        );

        Assert.Equal(5432, Assert.Single(targets).Port);
    }

    [Fact]
    public void Строка_подключения_без_хоста_не_даёт_цели()
    {
        var targets = DevInfrastructureTargetResolver.Resolve(
            Config(("ConnectionStrings:MediaDb", "Database=mars_media;Username=mars"))
        );

        Assert.Empty(targets);
    }

    /// <summary>
    /// У сервиса может быть несколько баз на одном хосте и порту: пробовать один и
    /// тот же порт несколько раз незачем.
    /// </summary>
    [Fact]
    public void Базы_одного_порта_схлопываются()
    {
        var targets = DevInfrastructureTargetResolver.Resolve(
            Config(
                ("ConnectionStrings:A", "Host=localhost;Port=6017;Database=a"),
                ("ConnectionStrings:B", "Host=localhost;Port=6017;Database=b")
            )
        );

        var target = Assert.Single(targets);
        Assert.Equal("ConnectionStrings:A", target.Source);
    }

    /// <summary>
    /// В контейнере хост — это имя службы compose. Такой адрес локальным не
    /// считается: именно на этом различии держится защита контейнеров от
    /// автостарта, потому что <c>IsDevelopment()</c> у них истинно.
    /// </summary>
    [Fact]
    public void Имя_службы_compose_не_считается_локальным()
    {
        var targets = DevInfrastructureTargetResolver.Resolve(
            Config(("ConnectionStrings:TwitchDb", "Host=postgres;Port=5432;Database=mars_twitch"))
        );

        Assert.False(Assert.Single(targets).IsLocal);
    }

    [Fact]
    public void Адрес_брокера_даёт_цель_rabbitmq()
    {
        var targets = DevInfrastructureTargetResolver.Resolve(
            Config(("RabbitMq:Host", "localhost"))
        );

        var target = Assert.Single(targets);
        Assert.Equal("rabbitmq", target.Service);
        Assert.Equal("localhost", target.Host);
        Assert.Equal(5672, target.Port);
        Assert.Equal("RabbitMq:Host", target.Source);
        Assert.True(target.IsLocal);
    }

    [Fact]
    public void Порт_брокера_берётся_из_конфигурации()
    {
        var targets = DevInfrastructureTargetResolver.Resolve(
            Config(("RabbitMq:Host", "localhost"), ("RabbitMq:Port", "6018"))
        );

        Assert.Equal(6018, Assert.Single(targets).Port);
    }

    /// <summary>
    /// Пустой <c>RabbitMq:Host</c> — это «брокер сервису не нужен»: потребители
    /// регистрируются только при непустом адресе.
    /// </summary>
    [Fact]
    public void Пустой_адрес_брокера_не_даёт_цели()
    {
        var targets = DevInfrastructureTargetResolver.Resolve(
            Config(("RabbitMq:Host", ""), ("RabbitMq:Port", "6018"))
        );

        Assert.Empty(targets);
    }

    [Fact]
    public void Адрес_otlp_даёт_цель_tempo()
    {
        var targets = DevInfrastructureTargetResolver.Resolve(
            Config(("Otlp:Endpoint", "http://localhost:6021"))
        );

        var target = Assert.Single(targets);
        Assert.Equal("tempo", target.Service);
        Assert.Equal("localhost", target.Host);
        Assert.Equal(6021, target.Port);
        Assert.Equal("Otlp:Endpoint", target.Source);
    }

    [Fact]
    public void Пустой_адрес_otlp_не_даёт_цели()
    {
        var targets = DevInfrastructureTargetResolver.Resolve(Config(("Otlp:Endpoint", "")));

        Assert.Empty(targets);
    }

    /// <summary>Профиль MARS.Local целиком: три адреса, по одному на службу.</summary>
    [Fact]
    public void Цели_разных_служб_не_схлопываются()
    {
        var targets = DevInfrastructureTargetResolver.Resolve(
            Config(
                ("ConnectionStrings:TwitchDb", "Host=localhost;Port=6017;Database=mars_twitch"),
                ("RabbitMq:Host", "localhost"),
                ("RabbitMq:Port", "6018"),
                ("Otlp:Endpoint", "http://localhost:6021")
            )
        );

        Assert.Equal(
            new[] { "postgres", "rabbitmq", "tempo" },
            targets.Select(target => target.Service)
        );
    }

    [Theory]
    [InlineData("localhost", true)]
    [InlineData("LOCALHOST", true)]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("[::1]", true)]
    [InlineData("postgres", false)]
    [InlineData("rabbitmq", false)]
    [InlineData("mars.example.com", false)]
    public void Локальными_считаются_только_адреса_самого_хоста(string host, bool expected)
    {
        Assert.Equal(expected, DevInfrastructureTarget.IsLocalHost(host));
    }

    [Fact]
    public void Пустой_хост_не_локальный()
    {
        Assert.False(DevInfrastructureTarget.IsLocalHost(null));
        Assert.False(DevInfrastructureTarget.IsLocalHost(string.Empty));
    }
}
