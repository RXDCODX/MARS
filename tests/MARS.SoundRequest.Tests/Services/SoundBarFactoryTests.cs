using System.Reflection;
using MARS.SoundRequest.Configuration;
using MARS.SoundRequest.Services.SoundBarService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.SoundRequest.Tests.Services;

/// <summary>
/// Выбор реализации звука и адреса контроллера.
///
/// Проверяется порт: в продакшене и в разработке он разный, и ошибка здесь
/// означала бы «звук не работает» без всякой диагностики.
/// </summary>
public class SoundBarFactoryTests
{
    [Theory]
    [InlineData("Development", 30691)]
    [InlineData("Production", 30695)]
    public void PortDependsOnEnvironment(string environmentName, int expected)
    {
        var factory = Create(environmentName, new HttpClientsConfiguration());

        Assert.EndsWith($"127.0.0.1:{expected}", AudioControllerUrl(factory));
    }

    [Theory]
    [InlineData("Development", 1234)]
    [InlineData("Production", 4321)]
    public void ConfiguredPortWins(string environmentName, int port)
    {
        var configuration = new HttpClientsConfiguration
        {
            AudioControllerDevPort = port,
            AudioControllerProdPort = port,
        };

        var factory = Create(environmentName, configuration);

        Assert.EndsWith($"127.0.0.1:{port}", AudioControllerUrl(factory));
    }

    /// <summary>
    /// Нулевой порт в настройках — это «не задано», а не «слушать на нулевом
    /// порту»: иначе контроллер оказался бы недостижим.
    /// </summary>
    [Fact]
    public void ZeroPortFallsBackToDefault()
    {
        var factory = Create(
            "Production",
            new HttpClientsConfiguration { AudioControllerDevPort = 0, AudioControllerProdPort = 0 }
        );

        Assert.EndsWith("127.0.0.1:30695", AudioControllerUrl(factory));
    }

    [Fact]
    public void SoundBarIsCreated()
    {
        var factory = Create("Development", new HttpClientsConfiguration());

        Assert.NotNull(factory.CreateSoundBar());
    }

    /// <summary>
    /// Адрес приватный и наружу не отдаётся: проверяется он напрямую, тем же
    /// способом, каким его вызывает <see cref="SoundBarFactory.CreateSoundBar"/>.
    /// </summary>
    private static string AudioControllerUrl(SoundBarFactory factory) =>
        (string)
            typeof(SoundBarFactory)
                .GetMethod("GetAudioControllerUrl", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(factory, null)!;

    private static SoundBarFactory Create(
        string environmentName,
        HttpClientsConfiguration configuration
    ) =>
        new(
            new StubEnvironment(environmentName),
            Mock.Of<IHttpClientFactory>(),
            NullLogger<SoundBarFactory>.Instance,
            Options.Create(configuration)
        );

    private sealed class StubEnvironment(string environmentName) : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "MARS.SoundRequest.Tests";

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public string EnvironmentName { get; set; } = environmentName;
    }
}
