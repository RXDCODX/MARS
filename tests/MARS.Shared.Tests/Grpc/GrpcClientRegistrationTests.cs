using MARS.Shared.Configuration;
using MARS.Shared.Extensions;
using MARS.Shared.Grpc.Telegramus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TelegramusServiceClient = MARS.Shared.Grpc.Telegramus.TelegramusService.TelegramusServiceClient;

namespace MARS.Shared.Tests.Grpc;

/// <summary>
/// Адрес gRPC выводится из адреса REST подменой порта, а не добавлением
/// свойства в <see cref="ServiceEndpoints"/>. Причина — рефлексия
/// <c>SwaggerEndpointMap.Build</c>: она обходит все строковые свойства и
/// приписывает <c>/swagger/v1/swagger.json</c>, поэтому свойство вида
/// <c>CommandsGrpc</c> заставило бы агрегатор лезть за спекой на порт,
/// который обслуживает только HTTP/2.
/// </summary>
public class GrpcClientRegistrationTests
{
    [Theory]
    [InlineData("http://commands:8080", "http://commands:8081/")]
    [InlineData("http://obs:8080/", "http://obs:8081/")]
    [InlineData("https://alerts:5000", "https://alerts:8081/")]
    public void GrpcAddressReplacesPortButKeepsSchemeAndHost(string rest, string expected)
    {
        Assert.Equal(expected, GrpcClientExtensions.WithGrpcPort(rest).ToString());
    }

    /// <summary>
    /// Порт gRPC объявлен кодом в <c>AddMarsGrpcHosting</c>, поэтому подменяется
    /// он всегда — даже если REST уехал на другой порт.
    /// </summary>
    [Fact]
    public void GrpcAddressAlwaysUsesKestrelGrpcPort()
    {
        Assert.Equal(8081, GrpcClientExtensions.WithGrpcPort("http://commands:3000").Port);
    }

    /// <summary>
    /// Путь и запрос в базовом адресе клиенту не нужны и приводят к 404 на
    /// вызове метода: адрес склеивается с путём метода.
    /// </summary>
    [Fact]
    public void GrpcAddressDropsPathAndQuery()
    {
        var address = GrpcClientExtensions.WithGrpcPort("http://commands:8080/api?x=1");

        Assert.Equal("/", address.AbsolutePath);
        Assert.Empty(address.Query);
    }

    [Fact]
    public void TypedClientResolvesWithGrpcAddress()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMarsGrpcClient<TelegramusServiceClient>(
            Options.Create(new ServiceEndpoints()),
            nameof(ServiceEndpoints.OBS)
        );

        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<TelegramusServiceClient>();

        Assert.NotNull(client);
    }

    /// <summary>
    /// Имя свойства ищется без учёта регистра: привязка секции конфигурации
    /// тоже регистронезависима, и строгая проверка расходилась бы с ней.
    /// </summary>
    [Fact]
    public void EndpointNameIsCaseInsensitive()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMarsGrpcClient<TelegramusServiceClient>(
            Options.Create(new ServiceEndpoints()),
            "obs"
        );

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(TelegramusServiceClient)
        );
    }

    /// <summary>
    /// Молчание здесь хуже падения: без адреса клиент уехал бы в default-порт
    /// localhost и выглядел бы как «сервис недоступен».
    /// </summary>
    [Fact]
    public void UnknownEndpointFailsLoudly()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMarsGrpcClient<TelegramusServiceClient>(
                Options.Create(new ServiceEndpoints()),
                "NoSuchService"
            )
        );
    }

    /// <summary>
    /// На хосте контейнерные номера не совпадают с хостовыми (REST у alerts —
    /// 6010, gRPC — 6028), поэтому порт нельзя выводить правилом «порт + 1»:
    /// адрес подставляется по имени сервиса из <see cref="GrpcOptions.Endpoints"/>.
    /// </summary>
    [Fact]
    public void ConfiguredEndpointAddressWinsOverPortHeuristic()
    {
        var grpc = new GrpcOptions();
        grpc.Endpoints["TTS"] = "http://localhost:6032";

        var address = GrpcClientExtensions.WithGrpcPort("http://tts:8080", grpc, "TTS");

        Assert.Equal("http://localhost:6032/", address.ToString());
    }

    [Fact]
    public void ConfiguredGrpcPortIsUsedWhenEndpointIsNotMapped()
    {
        var grpc = new GrpcOptions { GrpcPort = 9099 };

        var address = GrpcClientExtensions.WithGrpcPort("http://tts:8080", grpc, "TTS");

        Assert.Equal(9099, address.Port);
    }

    [Fact]
    public void EndpointAddressLookupIsCaseInsensitive()
    {
        var grpc = new GrpcOptions();
        grpc.Endpoints["tts"] = "http://localhost:6032";

        var address = GrpcClientExtensions.WithGrpcPort("http://tts:8080", grpc, "TTS");

        Assert.Equal(6032, address.Port);
    }

    /// <summary>
    /// Профиль запуска передаёт и <c>ServiceEndpoints</c>, и карту gRPC, поэтому
    /// регистрация обязана читать оба из конфигурации: иначе локальный клиент
    /// ушёл бы на контейнерный адрес и получил «сервис недоступен».
    /// </summary>
    [Fact]
    public void ClientRegistrationReadsAddressesFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ServiceEndpoints:TTS"] = "http://tts:8080",
                    ["Grpc:Endpoints:TTS"] = "http://localhost:6032",
                }
            )
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMarsGrpcClient<TelegramusServiceClient>(configuration, "TTS");

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(TelegramusServiceClient)
        );
    }

    [Fact]
    public void ClientRegistrationWithoutGrpcSectionFallsBackToContainerPort()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ServiceEndpoints:TTS"] = "http://tts:8080" }
            )
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMarsGrpcClient<TelegramusServiceClient>(configuration, "TTS");

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(TelegramusServiceClient)
        );
    }
}
