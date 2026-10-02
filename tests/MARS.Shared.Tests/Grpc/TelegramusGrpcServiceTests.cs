using Grpc.Core;
using Grpc.Net.Client;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Grpc.Services;
using MARS.Shared.Grpc.Telegramus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TelegramusServiceClient = MARS.Shared.Grpc.Telegramus.TelegramusService.TelegramusServiceClient;

namespace MARS.Shared.Tests.Grpc;

public class TelegramusGrpcServiceTests : IAsyncLifetime
{
    private static readonly TimeSpan StreamReadTimeout = TimeSpan.FromSeconds(10);

    private readonly GrpcEventBroadcaster<TelegramusEvent> _broadcaster = new();
    private readonly FakeAdhdConfigStore _adhdConfigStore = new();
    private ITelegramusNotifier _notifier = null!;
    private WebApplication _app = null!;
    private GrpcChannel _channel = null!;
    private TelegramusServiceClient _client = null!;

    /// <summary>
    /// Хранилище настройки раскладки в памяти: контракт проверяется, а не БД.
    /// </summary>
    private sealed class FakeAdhdConfigStore : IAdhdConfigStore
    {
        public string Current { get; set; } = """{"dvdLogosCount":12}""";

        public Task<string> GetAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Current);
        }

        public Task<string> UpdateAsync(string configJson, CancellationToken cancellationToken)
        {
            Current = configJson;
            return Task.FromResult(Current);
        }
    }

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.UseTestServer();
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(_broadcaster);
        builder.Services.AddSingleton<ITelegramusNotifier, TelegramusNotifier>();
        builder.Services.AddSingleton<IAdhdConfigStore>(_adhdConfigStore);

        _app = builder.Build();
        _app.MapGrpcService<TelegramusGrpcService>();

        await _app.StartAsync(TestContext.Current.CancellationToken);

        _notifier = _app.Services.GetRequiredService<ITelegramusNotifier>();
        _channel = GrpcChannel.ForAddress(
            "http://alerts.test",
            new GrpcChannelOptions { HttpHandler = _app.GetTestServer().CreateHandler() }
        );
        _client = new TelegramusServiceClient(_channel);
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Dispose();
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task Subscribe_ReceivesBroadcastEvents()
    {
        using var call = _client.Subscribe(
            new SubscribeRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        await WaitForSubscriberAsync();

        await _notifier.Explosion();

        Assert.True(await MoveNextAsync(call));

        Assert.Equal(
            TelegramusEvent.EventOneofCase.Explosion,
            call.ResponseStream.Current.EventCase
        );
    }

    [Fact]
    public async Task LogError_AndTwitchMsg_AreAccepted()
    {
        await _client.LogErrorAsync(
            new LogErrorRequest { ErrorMessage = "client failed" },
            cancellationToken: TestContext.Current.CancellationToken
        );

        var response = await _client.TwitchMsgAsync(
            new TwitchMsgRequest { Msg = "привет" },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.NotNull(response);
    }

    [Fact]
    public async Task Subscribe_StopsReceivingAfterClientDisconnects()
    {
        var call = _client.Subscribe(
            new SubscribeRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        await WaitForSubscriberAsync();

        call.Dispose();
        await WaitForSubscriberCountAsync(0);

        await _notifier.Explosion();

        Assert.Equal(0, _broadcaster.SubscriberCount);
    }

    /// <summary>
    /// <c>Fire</c> — единственный способ вбросить событие в broadcaster чужого
    /// процесса: сам broadcaster живёт в памяти сервиса, поэтому раньше
    /// оверлеем другого сервиса нельзя было управлять (например из команды /adhd).
    /// </summary>
    [Fact]
    public async Task Fire_DeliversEventToSubscribers()
    {
        using var call = _client.Subscribe(
            new SubscribeRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        await WaitForSubscriberAsync();

        await _client.FireAsync(
            new FireRequest { Event = new TelegramusEvent { Explosion = new EmptyEvent() } },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(await MoveNextAsync(call));

        Assert.Equal(
            TelegramusEvent.EventOneofCase.Explosion,
            call.ResponseStream.Current.EventCase
        );
    }

    /// <summary>
    /// <c>Fire</c> рассылает всем подписчикам, а не только первому: иначе один
    /// оверлей получил бы эффект, а второй — нет.
    /// </summary>
    [Fact]
    public async Task Fire_DeliversToEverySubscriber()
    {
        using var first = _client.Subscribe(
            new SubscribeRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        using var second = _client.Subscribe(
            new SubscribeRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );

        await WaitForSubscriberCountAsync(2);

        await _client.FireAsync(
            new FireRequest { Event = new TelegramusEvent { Explosion = new EmptyEvent() } },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(await MoveNextAsync(first));
        Assert.True(await MoveNextAsync(second));

        Assert.Equal(
            TelegramusEvent.EventOneofCase.Explosion,
            first.ResponseStream.Current.EventCase
        );
        Assert.Equal(
            TelegramusEvent.EventOneofCase.Explosion,
            second.ResponseStream.Current.EventCase
        );
    }

    /// <summary>
    /// Настройка раскладки читается из хранилища владельца таблицы: в монолите
    /// оверлей получал её вызовом ReceiveConfig сразу после подписки.
    /// </summary>
    [Fact]
    public async Task GetAdhdConfig_ReturnsConfigFromTheStore()
    {
        _adhdConfigStore.Current = """{"dvdLogosCount":5,"showRainEffect":false}""";

        var response = await _client.GetAdhdConfigAsync(
            new GetAdhdConfigRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(
            """{"dvdLogosCount":5,"showRainEffect":false}""",
            response.ConfigJson.ToStringUtf8()
        );
    }

    [Fact]
    public async Task UpdateAdhdConfig_WritesThroughToTheStore()
    {
        var response = await _client.UpdateAdhdConfigAsync(
            new UpdateAdhdConfigRequest
            {
                ConfigJson = Google.Protobuf.ByteString.CopyFromUtf8("""{"dvdLogosCount":3}"""),
                SubscriberId = "editor",
            },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal("""{"dvdLogosCount":3}""", response.ConfigJson.ToStringUtf8());
        Assert.Equal("""{"dvdLogosCount":3}""", _adhdConfigStore.Current);
    }

    /// <summary>
    /// Обновление уходит всем оверлеям, кроме того, который его внёс: в монолите
    /// это был Clients.Others.ConfigUpdated, а редактирующая вкладка и так
    /// получила записанное значение в ответе.
    /// </summary>
    [Fact]
    public async Task UpdateAdhdConfig_ReachesOtherSubscribersButNotTheAuthor()
    {
        using var author = _client.Subscribe(
            new SubscribeRequest { SubscriberId = "author" },
            cancellationToken: TestContext.Current.CancellationToken
        );
        using var viewer = _client.Subscribe(
            new SubscribeRequest { SubscriberId = "viewer" },
            cancellationToken: TestContext.Current.CancellationToken
        );

        await WaitForSubscriberCountAsync(2);

        await _client.UpdateAdhdConfigAsync(
            new UpdateAdhdConfigRequest
            {
                ConfigJson = Google.Protobuf.ByteString.CopyFromUtf8("""{"dvdLogosCount":3}"""),
                SubscriberId = "author",
            },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(await MoveNextAsync(viewer));
        Assert.Equal(
            TelegramusEvent.EventOneofCase.AdhdConfig,
            viewer.ResponseStream.Current.EventCase
        );

        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Поток автора молчит: по истёкшему токену MoveNext бросает RpcException
        // со статусом Cancelled, а не возвращает false — отсутствие события и
        // есть ожидаемый результат.
        var cancelled = await Assert.ThrowsAsync<RpcException>(
            async () => await author.ResponseStream.MoveNext(timeout.Token)
        );

        Assert.Equal(StatusCode.Cancelled, cancelled.StatusCode);
    }

    private static async Task<bool> MoveNextAsync(AsyncServerStreamingCall<TelegramusEvent> call)
    {
        using var timeout = new CancellationTokenSource(StreamReadTimeout);

        return await call.ResponseStream.MoveNext(timeout.Token);
    }

    /// <summary>
    /// Запрос подписки уходит лениво, при первом чтении потока: без этой паузы
    /// рассылка ушла бы в пустоту, и тест был бы плавающим.
    /// </summary>
    private async Task WaitForSubscriberAsync()
    {
        await WaitForSubscriberCountAsync(1);
    }

    private async Task WaitForSubscriberCountAsync(int expected)
    {
        using var timeout = new CancellationTokenSource(StreamReadTimeout);

        while (_broadcaster.SubscriberCount != expected)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }
}
