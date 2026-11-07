using MARS.Modules.Alerts.Twitch.Rewards.Application;
using MARS.Modules.Alerts.Twitch.Rewards.Application.Configuration;
using Microsoft.Extensions.Options;
using TL;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Проверяет конфигурацию, авторизацию и делегирование WTelegram-клиента без
/// сетевого подключения к Telegram.
/// </summary>
public sealed class WTelegramOnlineClientFactoryTests
{
    [Fact]
    public async Task CreateAsyncConfiguresAuthenticatesAndWrapsClient()
    {
        var adapter = new FakeWTelegramClientAdapter();
        var adapterFactory = new FakeWTelegramClientAdapterFactory(adapter);
        var factory = CreateFactory(adapterFactory);

        var client = await factory.CreateAsync(TestContext.Current.CancellationToken);

        Assert.True(adapter.LoginRequested);
        var configuration = Assert.IsType<Func<string, string?>>(adapterFactory.Configuration);
        Assert.Equal("123", configuration("api_id"));
        Assert.Equal("api-hash", configuration("api_hash"));
        Assert.Equal("+10000000000", configuration("phone_number"));
        Assert.Equal("password", configuration("password"));
        Assert.Null(configuration("unknown"));

        Func<UpdatesBase, Task> handler = _ => Task.CompletedTask;
        client.OnUpdates += handler;
        Assert.Equal(1, adapter.AddedHandlers);

        client.OnUpdates -= handler;
        Assert.Equal(1, adapter.RemovedHandlers);

        using var output = new MemoryStream();
        Assert.Equal(
            "document.mp4",
            await client.DownloadFileAsync(
                new Document
                {
                    id = 1,
                    size = 1,
                    mime_type = "video/mp4",
                },
                output
            )
        );
        Assert.Equal(
            "photo.jpg",
            await client.DownloadFileAsync(
                new Photo { id = 2 },
                output,
                new PhotoSize
                {
                    type = "x",
                    w = 1,
                    h = 1,
                    size = 1,
                }
            )
        );

        client.Dispose();
        Assert.True(adapter.Disposed);
    }

    [Fact]
    public async Task CreateAsyncHonorsCancellationBeforeCreatingClient()
    {
        var adapter = new FakeWTelegramClientAdapter();
        var adapterFactory = new FakeWTelegramClientAdapterFactory(adapter);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateFactory(adapterFactory).CreateAsync(cancellation.Token)
        );

        Assert.Null(adapterFactory.Configuration);
        Assert.False(adapter.LoginRequested);
    }

    private static WTelegramOnlineClientFactory CreateFactory(
        IWTelegramClientAdapterFactory clientFactory
    ) =>
        new(
            Options.Create(
                new WTelegramConfiguration
                {
                    AppId = 123,
                    ApiHash = "api-hash",
                    PhoneNumber = "+10000000000",
                    Password = "password",
                }
            ),
            clientFactory
        );

    private sealed class FakeWTelegramClientAdapterFactory(IWTelegramClientAdapter adapter)
        : IWTelegramClientAdapterFactory
    {
        public Func<string, string?>? Configuration { get; private set; }

        public IWTelegramClientAdapter Create(Func<string, string?> configuration)
        {
            Configuration = configuration;
            return adapter;
        }
    }

    private sealed class FakeWTelegramClientAdapter : IWTelegramClientAdapter
    {
        public bool LoginRequested { get; private set; }
        public bool Disposed { get; private set; }
        public int AddedHandlers { get; private set; }
        public int RemovedHandlers { get; private set; }

        public event Func<UpdatesBase, Task> OnUpdates
        {
            add => AddedHandlers++;
            remove => RemovedHandlers++;
        }

        public Task LoginUserIfNeeded()
        {
            LoginRequested = true;
            return Task.CompletedTask;
        }

        public Task<string> DownloadFileAsync(Document document, Stream output) =>
            Task.FromResult("document.mp4");

        public Task<string> DownloadFileAsync(Photo photo, Stream output, PhotoSize size) =>
            Task.FromResult("photo.jpg");

        public void Dispose() => Disposed = true;
    }
}
