using MARS.Alerts.Hubs;
using MARS.Alerts.Hubs.Interfaces;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Tuna;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace MARS.Alerts.Tests.Hubs;

/// <summary>
/// Реле хаба информации о треке.
/// </summary>
/// <remarks>
/// Проверки те же, что у реле оверлея, и по той же причине: хаба Tuna на сервере
/// не было вовсе, подписка клиента уходила в catch-all и получала HTML с кодом
/// 200. Сначала нужен хаб и реле, и только потом — тест, что событие доходит.
/// </remarks>
public class TunaHubRelayTests
{
    [Fact]
    public async Task Background_loop_forwards_tuna_event_to_hub()
    {
        var sent = new List<string>();
        var forwarded = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        var proxy = new Mock<IClientProxy>();
        proxy
            .Setup(p =>
                p.SendCoreAsync(
                    nameof(ITunaHub.TunaMusicInfo),
                    It.IsAny<object?[]>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask)
            .Callback<string, object?[], CancellationToken>(
                (_, _, _) =>
                {
                    lock (sent)
                    {
                        sent.Add(nameof(ITunaHub.TunaMusicInfo));
                        forwarded.TrySetResult();
                    }
                }
            );

        var broadcaster = new GrpcEventBroadcaster<TunaEvent>();
        var relay = new TunaHubRelay(broadcaster, HubContext(proxy));

        await relay.StartAsync(TestContext.Current.CancellationToken);
        await broadcaster.BroadcastAsync(new TunaEvent());

        await forwarded.Task.WaitAsync(
            TimeSpan.FromSeconds(60),
            TestContext.Current.CancellationToken
        );
        await relay.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal([nameof(ITunaHub.TunaMusicInfo)], sent);
    }

    /// <summary>
    /// Подписка обязана существовать сразу после возврата StartAsync.
    /// </summary>
    /// <remarks>
    /// Начиная с .NET 8 <c>BackgroundService.StartAsync</c> не ждёт тела фоновой
    /// задачи. Если бы подписка заводилась внутри <c>ExecuteAsync</c>, событие,
    /// опубликованное сразу после старта, ушло бы в никуда.
    /// </remarks>
    [Fact]
    public async Task Subscription_exists_immediately_after_start()
    {
        var broadcaster = new GrpcEventBroadcaster<TunaEvent>();
        var relay = new TunaHubRelay(broadcaster, HubContext(new Mock<IClientProxy>()));

        await relay.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, broadcaster.SubscriberCount);

        await relay.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Stopping_relay_removes_its_subscription()
    {
        var broadcaster = new GrpcEventBroadcaster<TunaEvent>();
        var relay = new TunaHubRelay(broadcaster, HubContext(new Mock<IClientProxy>()));

        await relay.StartAsync(TestContext.Current.CancellationToken);
        await relay.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, broadcaster.SubscriberCount);
    }

    private static IHubContext<TunaHub> HubContext(Mock<IClientProxy> proxy)
    {
        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.All).Returns(proxy.Object);
        var context = new Mock<IHubContext<TunaHub>>();
        context.Setup(c => c.Clients).Returns(clients.Object);

        return context.Object;
    }
}
