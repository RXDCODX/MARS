using MARS.Scoreboard.Hubs.Interfaces;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Scoreboard;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace MARS.Scoreboard.Hubs;

/// <summary>
/// Перекладывает события табло из broadcaster'а в методы хаба.
/// </summary>
/// <remarks>
/// Подписка заводится в <see cref="StartAsync"/>, а не в теле фоновой задачи:
/// начиная с .NET 8 <c>BackgroundService.StartAsync</c> не дожидается
/// <c>ExecuteAsync</c>, и событие, опубликованное сразу после старта хоста,
/// ушло бы в никуда.
/// </remarks>
public class ScoreboardHubRelay(
    GrpcEventBroadcaster<ScoreboardEvent> broadcaster,
    IHubContext<ScoreboardHub> hubContext
) : BackgroundService
{
    /// <summary>
    /// Имя подписки реле. На поведение не влияет: имя нужно только для
    /// исключения отправителя из рассылки.
    /// </summary>
    public const string SubscriberId = "scoreboard";

    /// <summary>
    /// Ёмкость очереди реле. Стандартных 64 хватает: события табло идут на
    /// каждое изменение счёта, и всплеска сотен записей не бывает.
    /// </summary>
    public const int SubscriptionCapacity = 128;

    private GrpcSubscription<ScoreboardEvent>? _subscription;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscription =
            _subscription ?? broadcaster.Subscribe(SubscriberId, SubscriptionCapacity);

        try
        {
            await foreach (var notification in subscription.Reader.ReadAllAsync(stoppingToken))
            {
                await DispatchAsync(notification, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Остановка хоста: очередь закрыта вместе с токеном.
        }
    }

    /// <summary>
    /// Отправляет событие всем подписчикам хаба.
    /// </summary>
    /// <remarks>
    /// Имя метода берётся из <c>nameof</c> по контракту хаба: у
    /// <c>IHubContext</c> типизированных методов нет, а строка в
    /// <c>SendCoreAsync</c> не анонимна.
    /// </remarks>
    private Task DispatchAsync(ScoreboardEvent notification, CancellationToken cancellationToken) =>
        notification.EventCase switch
        {
            ScoreboardEvent.EventOneofCase.StateUpdated => hubContext.Clients.All.SendCoreAsync(
                nameof(IScoreboardHub.ReceiveState),
                [notification.StateUpdated],
                cancellationToken
            ),
            // Остальные ветки oneof описывают точечные изменения счёта и
            // видимости. Панель адмистра перерисовывается по полному состоянию,
            // и отдельные методы под них не заводились: неиспользуемый метод
            // контракта — это обещание, которое никто не выполняет.
            _ => Task.CompletedTask,
        };

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _subscription ??= broadcaster.Subscribe(SubscriberId, SubscriptionCapacity);

        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        _subscription?.Dispose();
        _subscription = null;
    }
}
