using MARS.Platform.Infrastructure.Events;
using MARS.Scoreboard.Api.Hubs.Interfaces;
using MARS.Shared.Contracts.Scoreboard;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace MARS.Scoreboard.Api.Hubs;

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
    EventBroadcaster<ScoreboardEvent> broadcaster,
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

    private EventSubscription<ScoreboardEvent>? _subscription;

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
    /// <summary>
    /// Исключение из правила «имя ветки = имя метода»: клиент исторически
    /// подписан на <c>ReceiveState</c>. Остальные ветки точечных изменений
    /// намеренно не имеют методов — панель перерисовывается по полному состоянию.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> MethodNames = new Dictionary<
        string,
        string
    >(StringComparer.Ordinal)
    {
        [nameof(ScoreboardEvent.StateUpdated)] = nameof(IScoreboardHub.ReceiveState),
    };

    private Task DispatchAsync(ScoreboardEvent notification, CancellationToken cancellationToken) =>
        OneofHubRelay.DispatchAsync<ScoreboardEvent, IScoreboardHub>(
            notification,
            hubContext.Clients,
            MethodNames,
            payloadTransform: null,
            cancellationToken
        );

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
