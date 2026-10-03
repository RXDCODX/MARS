using MARS.Alerts.Hubs.Interfaces;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Tuna;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace MARS.Alerts.Hubs;

/// <summary>
/// Перекладывает события информации о треке из broadcaster'а в методы хаба.
/// </summary>
/// <remarks>
/// Устроен так же, как <see cref="HubEventRelay"/>, и по той же причине
/// подписка заводится в <see cref="StartAsync"/>, а не в теле фоновой задачи:
/// начиная с .NET 8 <c>BackgroundService.StartAsync</c> не дожидается
/// <c>ExecuteAsync</c>, и событие, опубликованное сразу после старта хоста,
/// уходило бы в никуда.
/// </remarks>
public class TunaHubRelay(
    GrpcEventBroadcaster<TunaEvent> broadcaster,
    IHubContext<TunaHub> hubContext
) : BackgroundService
{
    /// <summary>
    /// Имя подписки реле. На поведение не влияет: имя нужно только для
    /// исключения отправителя из рассылки.
    /// </summary>
    public const string SubscriberId = "tuna";

    /// <summary>
    /// Ёмкость очереди реле. Стандартных 64 хватает: события трека идут
    /// редко, всплеска быть не может — в отличие от наград у оверлея.
    /// </summary>
    public const int SubscriptionCapacity = 64;

    private GrpcSubscription<TunaEvent>? _subscription;

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
    /// Имя метода берётся из <c>nameof</c> по <see cref="ITunaHub"/>: у
    /// <c>IHubContext</c> типизированных методов нет, а строка в
    /// <c>SendCoreAsync</c> не анонимна — переименование метода в интерфейсе
    /// ломает сборку.
    /// </remarks>
    private Task DispatchAsync(TunaEvent notification, CancellationToken cancellationToken) =>
        hubContext.Clients.All.SendCoreAsync(
            nameof(ITunaHub.TunaMusicInfo),
            [notification],
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
