using MARS.Shared.Grpc;
using MARS.Shared.Grpc.SoundRequest;
using MARS.SoundRequest.Hubs;
using MARS.SoundRequest.Hubs.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace MARS.SoundRequest.Tests.Hubs;

/// <summary>
/// Реле хаба звуковых запросов.
/// </summary>
/// <remarks>
/// Хаба на сервере не было, хотя широковещатель и gRPC-сервис с той же полезной
/// нагрузкой уже были: клиент подписывался на путь, который никто не
/// обслуживал, и получал отказ. Тест проверяет, что событие из broadcaster'а
/// доходит до браузера методом контракта.
/// </remarks>
public class SoundRequestHubRelayTests
{
    [Fact]
    public async Task Player_state_event_reaches_hub()
    {
        var sent = new List<string>();
        var forwarded = NewSignal();

        var broadcaster = new GrpcEventBroadcaster<SoundRequestEvent>();
        var relay = new SoundRequestHubRelay(broadcaster, HubContext(forwarded, sent));

        await relay.StartAsync(TestContext.Current.CancellationToken);

        await broadcaster.BroadcastAsync(
            new SoundRequestEvent { PlayerStateChange = new PlayerStateSnapshot { IsMuted = true } }
        );

        await forwarded.Task.WaitAsync(
            TimeSpan.FromSeconds(60),
            TestContext.Current.CancellationToken
        );
        await relay.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal([nameof(ISoundRequestHub.PlayerStateChange)], sent);
    }

    [Fact]
    public async Task Queue_event_reaches_hub()
    {
        var sent = new List<string>();
        var forwarded = NewSignal();

        var broadcaster = new GrpcEventBroadcaster<SoundRequestEvent>();
        var relay = new SoundRequestHubRelay(broadcaster, HubContext(forwarded, sent));

        await relay.StartAsync(TestContext.Current.CancellationToken);

        await broadcaster.BroadcastAsync(new SoundRequestEvent { QueueChanged = new QueueState() });

        await forwarded.Task.WaitAsync(
            TimeSpan.FromSeconds(60),
            TestContext.Current.CancellationToken
        );
        await relay.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal([nameof(ISoundRequestHub.QueueChanged)], sent);
    }

    /// <summary>
    /// Подписка обязана существовать сразу после возврата StartAsync: начиная с
    /// .NET 8 он не дожидается тела фоновой задачи, и событие, опубликованное
    /// сразу после старта, ушло бы в никуда.
    /// </summary>
    [Fact]
    public async Task Subscription_exists_immediately_after_start()
    {
        var broadcaster = new GrpcEventBroadcaster<SoundRequestEvent>();
        var relay = new SoundRequestHubRelay(broadcaster, HubContext(NewSignal(), []));

        await relay.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, broadcaster.SubscriberCount);

        await relay.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Stopping_relay_removes_its_subscription()
    {
        var broadcaster = new GrpcEventBroadcaster<SoundRequestEvent>();
        var relay = new SoundRequestHubRelay(broadcaster, HubContext(NewSignal(), []));

        await relay.StartAsync(TestContext.Current.CancellationToken);
        await relay.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, broadcaster.SubscriberCount);
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static IHubContext<SoundRequestHub> HubContext(
        TaskCompletionSource forwarded,
        List<string> sent
    )
    {
        var proxy = new Mock<IClientProxy>();
        proxy
            .Setup(p =>
                p.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object?[]>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask)
            .Callback<string, object?[], CancellationToken>(
                (method, _, _) =>
                {
                    lock (sent)
                    {
                        sent.Add(method);
                        forwarded.TrySetResult();
                    }
                }
            );

        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.All).Returns(proxy.Object);
        var context = new Mock<IHubContext<SoundRequestHub>>();
        context.Setup(c => c.Clients).Returns(clients.Object);

        return context.Object;
    }
}
