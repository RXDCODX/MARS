using MARS.Telegram.Infrastructure.MTProto.Messaging;
using MARS.Telegram.Infrastructure.MTProto.PrivateChannelsResender;
using TL;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Тонкие обёртки над WTelegramClient: проверяем локальную проводку событий,
/// состояние авторизации и освобождение клиента без сетевых запросов.
/// </summary>
public sealed class WTelegramAdapterTests
{
    private static WTelegram.Client CreateClient() =>
        new(
            key =>
                key switch
                {
                    "api_id" => "123456",
                    "api_hash" => "0123456789abcdef0123456789abcdef",
                    _ => null,
                },
            new MemoryStream()
        );

    [Fact]
    public void ChannelClientSubscribesAndUnsubscribesFromUpdates()
    {
        var client = CreateClient();
        var adapter = new WTelegramChannelClient(client);
        Func<UpdatesBase, Task> handler = _ => Task.CompletedTask;

        adapter.OnUpdates += handler;
        adapter.OnUpdates -= handler;

        Assert.False(adapter.IsAuthorized);
        adapter.Dispose();
    }

    [Fact]
    public void BotConnectionDisposesUnderlyingClient()
    {
        var client = CreateClient();
        var adapter = new WTelegramBotConnection(client);

        adapter.Dispose();
    }
}
