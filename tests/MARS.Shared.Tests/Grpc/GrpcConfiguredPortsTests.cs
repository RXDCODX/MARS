using System.Globalization;
using Grpc.Net.Client;
using MARS.Shared.Configuration;
using MARS.Shared.Extensions;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Services;
using MARS.Shared.Grpc.Telegramus;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TelegramusServiceClient = MARS.Shared.Grpc.Telegramus.TelegramusService.TelegramusServiceClient;

namespace MARS.Shared.Tests.Grpc;

/// <summary>
/// <c>AddMarsGrpcHosting</c> слушает порты из <c>Grpc:HttpPort</c>/<c>Grpc:GrpcPort</c>,
/// а не жёстко 8080/8081. Проверка идёт через настоящий сокет: занятость 8080/8081
/// на машине разработчика — норма (там уже стоит стенд), и локальный запуск из IDE
/// обязан подниматься на свободных портах из профиля.
///
/// Порты фиксированы, а не случайны: адрес при случайном порте пришлось бы
/// вычитывать из <c>IServerAddressesFeature</c>, что к самому изменению отношения
/// не имеет.
/// </summary>
public sealed class GrpcConfiguredPortsTests : IAsyncLifetime
{
    private const int HttpPort = 50997;
    private const int GrpcPort = 50998;

    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(10);

    private WebApplication _app = null!;
    private GrpcChannel _channel = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Configuration[GrpcOptions.SectionName + ":HttpPort"] = HttpPort.ToString(
            CultureInfo.InvariantCulture
        );
        builder.Configuration[GrpcOptions.SectionName + ":GrpcPort"] = GrpcPort.ToString(
            CultureInfo.InvariantCulture
        );

        builder.AddMarsGrpcHosting();
        builder.Services.AddSingleton<GrpcEventBroadcaster<TelegramusEvent>>();

        _app = builder.Build();
        _app.MapGrpcService<TelegramusGrpcService>();

        await _app.StartAsync(TestContext.Current.CancellationToken);

        _channel = GrpcChannel.ForAddress($"http://127.0.0.1:{GrpcPort}");
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Dispose();
        await _app.StopAsync(CancellationToken.None);
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task GrpcListensOnConfiguredPort()
    {
        var client = new TelegramusServiceClient(_channel);

        using var timeout = new CancellationTokenSource(CallTimeout);

        var response = await client.LogErrorAsync(
            new LogErrorRequest { ErrorMessage = "проверка настраиваемого порта" },
            cancellationToken: timeout.Token
        );

        Assert.NotNull(response);
    }
}
