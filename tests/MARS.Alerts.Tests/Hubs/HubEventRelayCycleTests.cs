using MARS.Alerts.Hubs;
using MARS.Alerts.Hubs.Interfaces;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Telegramus;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace MARS.Alerts.Tests.Hubs;

/// <summary>
/// Фоновый цикл реле, а не только раскладка событий.
/// </summary>
/// <remarks>
/// Ожидание построено на признаке, а не на <c>Task.Delay</c>: после
/// <c>StartAsync</c> счётчик отправок ещё нулевой, потому что подписка и чтение
/// из очереди происходят внутри фоновой задачи. Ждать пришлось бы по числу
/// попыток, а ждать настоящий интервал в тесте нельзя.
/// </remarks>
public class HubEventRelayCycleTests
{
    private const int AttemptsLimit = 200;
    private static readonly TimeSpan AttemptDelay = TimeSpan.FromMilliseconds(20);

    /// <summary>
    /// Событие из broadcaster'а доходит до хаба без участия gRPC-стрима.
    /// Именно это делает браузер работоспособным: до появления реле оверлей
    /// получал события только по <c>Subscribe</c> на 8081, а хаб на 8080 молчал.
    /// </summary>
    [Fact]
    public async Task Background_loop_forwards_broadcast_event_to_hub()
    {
        var sent = new List<string>();
        var forwarded = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        var proxy = new Mock<IClientProxy>();
        proxy
            .Setup(p =>
                p.SendCoreAsync(
                    nameof(ITelegramusHub.Credits),
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
                        sent.Add(nameof(ITelegramusHub.Credits));
                        forwarded.TrySetResult();
                    }
                }
            );

        var broadcaster = new GrpcEventBroadcaster<TelegramusEvent>();
        var relay = StartRelay(broadcaster, proxy);

        await relay.StartAsync(TestContext.Current.CancellationToken);
        await broadcaster.BroadcastAsync(new TelegramusEvent { Credits = new EmptyEvent() });

        // Ожидание по признаку с запасом, а не гонка с фиксированным Task.Delay.
        // Десяти секунд на инструментированном CI-раннере не хватало: тест падал
        // не по существу, а Assert.Same получал задачу таймаута и сообщал
        // «values are not the same instance» про два Task, между которыми и не
        // было разницы по смыслу. WaitAsync даёт внятную причину — TimeoutException.
        await forwarded.Task.WaitAsync(
            TimeSpan.FromSeconds(60),
            TestContext.Current.CancellationToken
        );

        await relay.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal([nameof(ITelegramusHub.Credits)], sent);
    }

    /// <summary>
    /// Остановка хоста закрывает очередь подписки. Без этого реле держало бы
    /// подписку в словаре broadcaster'а после перезапуска, и её канал уехал бы в
    /// сторону вместе с потерянными на ней событиями.
    /// </summary>
    [Fact]
    public async Task Stopping_relay_removes_its_subscription()
    {
        var broadcaster = new GrpcEventBroadcaster<TelegramusEvent>();
        var relay = StartRelay(broadcaster, new Mock<IClientProxy>());

        await relay.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => broadcaster.SubscriberCount == 1);

        await relay.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, broadcaster.SubscriberCount);
    }

    /// <summary>
    /// Ожидание по числу попыток, а не по одному <c>Task.Delay</c>: фоновый
    /// цикл стартует асинхронно, и сразу после <c>StartAsync</c> подписки ещё нет.
    /// </summary>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < AttemptsLimit; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(AttemptDelay, TestContext.Current.CancellationToken);
        }

        Assert.Fail($"Условие не выполнилось за {AttemptsLimit} попыток");
    }

    private static HubEventRelay StartRelay(
        GrpcEventBroadcaster<TelegramusEvent> broadcaster,
        Mock<IClientProxy> proxy
    )
    {
        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.All).Returns(proxy.Object);
        var context = new Mock<IHubContext<OverlayHub>>();
        context.Setup(c => c.Clients).Returns(clients.Object);

        return new HubEventRelay(broadcaster, context.Object);
    }
}
