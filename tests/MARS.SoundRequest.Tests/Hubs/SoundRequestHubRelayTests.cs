using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.SoundRequest;
using MARS.SoundRequest.Api.Hubs;
using MARS.SoundRequest.Api.Hubs.Interfaces;
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

        var broadcaster = new EventBroadcaster<SoundRequestEvent>();
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

        var broadcaster = new EventBroadcaster<SoundRequestEvent>();
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
        var broadcaster = new EventBroadcaster<SoundRequestEvent>();
        var relay = new SoundRequestHubRelay(broadcaster, HubContext(NewSignal(), []));

        await relay.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, broadcaster.SubscriberCount);

        await relay.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Stopping_relay_removes_its_subscription()
    {
        var broadcaster = new EventBroadcaster<SoundRequestEvent>();
        var relay = new SoundRequestHubRelay(broadcaster, HubContext(NewSignal(), []));

        await relay.StartAsync(TestContext.Current.CancellationToken);
        await relay.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, broadcaster.SubscriberCount);
    }

    /// <summary>
    /// Каждая ветка события уходит в одноимённый метод хаба; аргумент
    /// преобразуется по сигнатуре контракта. Список веток — из рефлексии.
    /// </summary>
    [Fact]
    public async Task Every_branch_reaches_its_hub_method()
    {
        foreach (var branch in OneofEventContract.Branches(typeof(SoundRequestEvent)))
        {
            var sent = new List<(string Method, object? Payload)>();
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
                    (method, args, _) => sent.Add((method, args.FirstOrDefault()))
                );
            var clients = new Mock<IHubClients>();
            clients.Setup(c => c.All).Returns(proxy.Object);

            var notification = new SoundRequestEvent();
            branch.SetValue(notification, Activator.CreateInstance(branch.PropertyType));

            await OneofHubRelay.DispatchAsync<SoundRequestEvent, ISoundRequestHub>(
                notification,
                clients.Object,
                methodNames: null,
                (name, payload) =>
                    name == nameof(SoundRequestEvent.PlayerStateChange)
                        ? SoundRequestHubMapper.ToHubState((PlayerStateSnapshot)payload!)
                        : payload,
                TestContext.Current.CancellationToken
            );

            var call = Assert.Single(sent);
            Assert.Equal(branch.Name, call.Method);
            Assert.NotNull(call.Payload);
        }
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
