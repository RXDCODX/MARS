using MARS.Scoreboard.Hubs;
using MARS.Scoreboard.Hubs.Interfaces;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Scoreboard;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace MARS.Scoreboard.Tests.Hubs;

/// <summary>
/// Реле хаба табло.
/// </summary>
/// <remarks>
/// Хаба на сервере не было, хотя широковещатель и gRPC-сервис с той же
/// полезной нагрузкой уже были. Тест проверяет, что состояние табло доходит до
/// браузера методом контракта.
/// </remarks>
public class ScoreboardHubRelayTests
{
    [Fact]
    public async Task State_event_reaches_hub()
    {
        var sent = new List<string>();
        var forwarded = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        var broadcaster = new GrpcEventBroadcaster<ScoreboardEvent>();
        var relay = new ScoreboardHubRelay(broadcaster, HubContext(forwarded, sent));

        await relay.StartAsync(TestContext.Current.CancellationToken);

        await broadcaster.BroadcastAsync(
            new ScoreboardEvent { StateUpdated = new ScoreboardSnapshot { IsVisible = true } }
        );

        await forwarded.Task.WaitAsync(
            TimeSpan.FromSeconds(60),
            TestContext.Current.CancellationToken
        );
        await relay.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal([nameof(IScoreboardHub.ReceiveState)], sent);
    }

    /// <summary>
    /// Подписка обязана существовать сразу после возврата StartAsync: начиная с
    /// .NET 8 он не дожидается тела фоновой задачи.
    /// </summary>
    [Fact]
    public async Task Subscription_exists_immediately_after_start()
    {
        var broadcaster = new GrpcEventBroadcaster<ScoreboardEvent>();
        var relay = new ScoreboardHubRelay(broadcaster, HubContext(NewSignal(), []));

        await relay.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, broadcaster.SubscriberCount);

        await relay.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Stopping_relay_removes_its_subscription()
    {
        var broadcaster = new GrpcEventBroadcaster<ScoreboardEvent>();
        var relay = new ScoreboardHubRelay(broadcaster, HubContext(NewSignal(), []));

        await relay.StartAsync(TestContext.Current.CancellationToken);
        await relay.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, broadcaster.SubscriberCount);
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static IHubContext<ScoreboardHub> HubContext(
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
        var context = new Mock<IHubContext<ScoreboardHub>>();
        context.Setup(c => c.Clients).Returns(clients.Object);

        return context.Object;
    }
}
