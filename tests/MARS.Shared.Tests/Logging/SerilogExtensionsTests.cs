using MARS.Shared.Logging;
using Microsoft.Extensions.Configuration;

namespace MARS.Shared.Tests.Logging;

/// <summary>
/// Два пути наблюдаемости не должны сходиться в один эндпоинт: трейсы идут на
/// Tempo через <c>Otlp:Endpoint</c>, а логи — на Alloy через
/// <c>Serilog:OtlpEndpoint</c>, откуда Alloy раскладывает их в Loki.
///
/// Имя контейнера в compose — kebab-case (<c>twitch-core</c>), а сервис
/// представляется как <c>MARS.TwitchCore</c>; Alloy превращает
/// <c>service.name</c> в метку Loki, а Grafana ищет по метке <c>container</c>,
/// поэтому соответствие имён и есть контракт лейблов.
/// </summary>
public class SerilogExtensionsTests
{
    [Fact]
    public void LogEndpointIsReadFromSerilogSection()
    {
        var configuration = Configuration(
            ("Serilog:OtlpEndpoint", "http://alloy:4317"),
            ("Otlp:Endpoint", "http://tempo:4317")
        );

        Assert.Equal("http://alloy:4317", SerilogExtensions.ResolveLogEndpoint(configuration));
    }

    /// <summary>
    /// Старое имя остаётся запасным: сервис, у которого ещё не задан
    /// <c>Serilog__OtlpEndpoint</c>, не должен остаться без логов вовсе.
    /// </summary>
    [Fact]
    public void LogEndpointFallsBackToOtlpSection()
    {
        var configuration = Configuration(("Otlp:Endpoint", "http://tempo:4317"));

        Assert.Equal("http://tempo:4317", SerilogExtensions.ResolveLogEndpoint(configuration));
    }

    /// <summary>
    /// Пустая строка — незаданное значение: compose подставляет её для
    /// отсутствующей переменной, и она не должна перебивать запасной ключ.
    /// </summary>
    [Fact]
    public void BlankSerilogEndpointFallsBackToOtlpSection()
    {
        var configuration = Configuration(
            ("Serilog:OtlpEndpoint", ""),
            ("Otlp:Endpoint", "http://tempo:4317")
        );

        Assert.Equal("http://tempo:4317", SerilogExtensions.ResolveLogEndpoint(configuration));
    }

    [Fact]
    public void MissingEndpointsYieldNull()
    {
        Assert.Null(SerilogExtensions.ResolveLogEndpoint(Configuration()));
    }

    [Theory]
    [InlineData("MARS.TwitchCore", "twitch-core")]
    [InlineData("MARS.MediaStorage", "media-storage")]
    [InlineData("MARS.TTS", "tts")]
    [InlineData("MARS.Videos365", "videos365")]
    [InlineData("MARS.Scoreboard", "scoreboard")]
    public void ContainerNameIsKebabCaseComposeServiceName(string serviceName, string expected)
    {
        Assert.Equal(expected, SerilogExtensions.ToContainerName(serviceName));
    }

    private static IConfiguration Configuration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                values.Select(value => new KeyValuePair<string, string?>(value.Key, value.Value))
            )
            .Build();
}
