using Grpc.Net.Client;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Services;
using MARS.Shared.Grpc.Telegramus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TelegramusServiceClient = MARS.Shared.Grpc.Telegramus.TelegramusService.TelegramusServiceClient;

namespace MARS.Shared.Tests.Grpc;

/// <summary>
/// Проверяет, что gRPC работает по <b>h2c</b> — HTTP/2 без TLS — через настоящий
/// сокет, потому что остальные тесты идут через TestServer и подменяют Kestrel
/// целиком, а значит согласование протокола не проверяют вовсе.
///
/// Это единственный способ узнать, нужен ли клиенту
/// <c>AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true)</c>:
/// клиентских каналов в репозитории не было ни одного, и на живом запуске путь
/// не проверялся. Эндпоинт поднят с Protocols = Http2 — тем же способом, что
/// и <c>AddMarsGrpcHosting</c> поднимает 8081.
/// </summary>
public sealed class H2cTransportTests : IAsyncLifetime
{
    // Порт не 0: при 0 адрес назначается случайно и его нужно вычитывать из
    // IServerAddressesFeature, что добавляет к тесту ничего, кроме хрупкости.
    private const int GrpcPort = 50999;

    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(10);

    private WebApplication _app = null!;
    private GrpcChannel _channel = null!;
    private string? _observedProtocol;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenLocalhost(GrpcPort, listen => listen.Protocols = HttpProtocols.Http2);
        });
        builder.Services.AddGrpc();
        builder.Services.AddSingleton<GrpcEventBroadcaster<TelegramusEvent>>();
        builder.Services.AddSingleton<IAdhdConfigStore, UnavailableAdhdConfigStore>();

        _app = builder.Build();
        _app.Use(
            async (context, next) =>
            {
                _observedProtocol = context.Request.Protocol;

                await next(context);
            }
        );
        _app.MapGrpcService<TelegramusGrpcService>();

        await _app.StartAsync(TestContext.Current.CancellationToken);

        _channel = GrpcChannel.ForAddress($"http://127.0.0.1:{GrpcPort}");
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Dispose();
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
    }

    /// <summary>
    /// Рабочий unary-вызов по h2c. Если transport негоден, исключение приходит
    /// как <c>HttpRequestException</c> или <c>RpcException</c> со статусом
    /// <c>Unimplemented</c> — раньше это читалось как «сервис недоступен».
    /// </summary>
    [Fact]
    public async Task UnaryCallSucceedsOverCleartextHttp2()
    {
        var client = new TelegramusServiceClient(_channel);

        using var timeout = new CancellationTokenSource(CallTimeout);

        var response = await client.LogErrorAsync(
            new LogErrorRequest { ErrorMessage = "проверка h2c" },
            cancellationToken: timeout.Token
        );

        Assert.NotNull(response);
    }

    /// <summary>
    /// Явно зафиксирован HTTP/2. Эндпоинт поднят как <c>Protocols = Http2</c>,
    /// поэтому HTTP/1.1 не прошёл бы вовсе — но если конфиг однажды ослабят до
    /// <c>Http1AndHttp2</c>, тест выше начнёт проходить, ни разу не проверив h2c.
    /// </summary>
    [Fact]
    public async Task RequestIsServedOverHttp2()
    {
        var client = new TelegramusServiceClient(_channel);

        using var timeout = new CancellationTokenSource(CallTimeout);

        await client.TwitchMsgAsync(
            new TwitchMsgRequest { Msg = "проверка протокола" },
            cancellationToken: timeout.Token
        );

        Assert.Equal("HTTP/2", _observedProtocol);
    }
}
