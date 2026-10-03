using MARS.Shared.Grpc;
using MARS.Shared.Grpc.SoundRequest;
using MARS.SoundRequest.Grpc;
using MARS.SoundRequest.Hubs.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace MARS.SoundRequest.Hubs;

/// <summary>
/// Перекладывает события звуковых запросов из broadcaster'а в методы хаба.
/// </summary>
/// <remarks>
/// Устроен так же, как реле хаба трека. Подписка заводится в
/// <see cref="StartAsync"/>, а не в теле фоновой задачи: начиная с .NET 8
/// <c>BackgroundService.StartAsync</c> не дожидается <c>ExecuteAsync</c>, и
/// событие, опубликованное сразу после старта хоста, ушло бы в никуда.
/// </remarks>
public class SoundRequestHubRelay(
    GrpcEventBroadcaster<SoundRequestEvent> broadcaster,
    IHubContext<SoundRequestHub> hubContext
) : BackgroundService
{
    /// <summary>
    /// Имя подписки реле. На поведение не влияет: имя нужно только для
    /// исключения отправителя из рассылки.
    /// </summary>
    public const string SubscriberId = "sound-request";

    /// <summary>
    /// Ёмкость очереди реле. Стандартных 64 хватает: события приходят на
    /// каждое изменение очереди, и всплеска из сотен записей не бывает —
    /// очередь на приёмной стороне мала.
    /// </summary>
    public const int SubscriptionCapacity = 128;

    private GrpcSubscription<SoundRequestEvent>? _subscription;

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
    /// <c>SendCoreAsync</c> не анонимна — переименование метода ломает сборку.
    /// <para>
    /// Состояние плеера уходит в форме <c>PlayerStateHubDto</c>, а не снимком
    /// из proto. Клиент читает <c>currentTrackProgress</c> строкой
    /// <c>hh:mm:ss</c> и <c>stateVersion</c>, а в снимке прогресс в целых
    /// секундах и имени версии нет вовсе. Отсюда было, что видеоэкран получал
    /// <c>undefined</c> вместо прогресса и начинал с нуля, а пульт отправлял
    /// обратно объект без прогресса — и каждое нажатие play или mute обнуляло
    /// прогресс трека в состоянии сервиса.
    /// </para>
    /// </remarks>
    private Task DispatchAsync(
        SoundRequestEvent notification,
        CancellationToken cancellationToken
    ) =>
        notification.EventCase switch
        {
            SoundRequestEvent.EventOneofCase.PlayerStateChange =>
                hubContext.Clients.All.SendCoreAsync(
                    nameof(ISoundRequestHub.PlayerStateChange),
                    [SoundRequestHubMapper.ToHubState(notification.PlayerStateChange)],
                    cancellationToken
                ),
            SoundRequestEvent.EventOneofCase.QueueChanged => hubContext.Clients.All.SendCoreAsync(
                nameof(ISoundRequestHub.QueueChanged),
                [notification.QueueChanged],
                cancellationToken
            ),
            // Ветка None: сообщение без события. Отправлять некуда.
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
