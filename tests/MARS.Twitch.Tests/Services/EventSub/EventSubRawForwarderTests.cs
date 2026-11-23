using MARS.Twitch.Infrastructure.Services.EventSub;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.EventSub.Websockets;
using Wolverine;

namespace MARS.TwitchCore.Tests.Services.EventSub;

/// <summary>
/// <see cref="EventSubRawForwarder"/> — IHostedService, пересылает сырые фреймы
/// EventSub в шину. Проверяется, что сервис поднимается без исключений даже
/// при отсутствии подключённого websocket'а.
/// </summary>
public class EventSubRawForwarderTests
{
    [Fact]
    public async Task StartAsyncCompletesWithoutThrowing()
    {
        var wsClient = new EventSubWebsocketClient();
        var bus = Mock.Of<Wolverine.IMessageBus>();
        var services = new ServiceCollection();
        services.AddSingleton(bus);
        var scopeFactory = services
            .BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();
        var logger = NullLogger<EventSubRawForwarder>.Instance;

        var service = new EventSubRawForwarder(wsClient, scopeFactory, logger);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        Assert.True(true);
    }
}
