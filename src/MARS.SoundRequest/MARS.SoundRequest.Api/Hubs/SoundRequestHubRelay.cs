using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts.SoundRequest;
using MARS.SoundRequest;
using MARS.SoundRequest.Api.Hubs.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace MARS.SoundRequest.Api.Hubs;

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
    EventBroadcaster<SoundRequestEvent> broadcaster,
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

    private EventSubscription<SoundRequestEvent>? _subscription;

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
    /// Имя метода берётся из ветки события, а наличие аргумента — из сигнатуры
    /// контракта хаба; <c>ISoundRequestHub</c> — единственное место, где имя
    /// метода определено, и её переименование ломает и клиент, и это реле.
    /// <para>
    /// Состояние плеера уходит в форме <c>PlayerStateHubDto</c>, а не снимком
    /// из proto. Клиент читает <c>currentTrackProgress</c> строкой
    /// <c>hh:mm:ss</c>, а в снимке прогресс в целых
    /// секундах и имени версии нет вовсе. Отсюда было, что видеоэкран получал
    /// <c>undefined</c> вместо прогресса и начинал с нуля, а пульт отправлял
    /// обратно объект без прогресса — и каждое нажатие play или mute обнуляло
    /// прогресс трека в состоянии сервиса.
    /// </para>
    /// </remarks>
    private static object? TransformPayload(string branch, object? payload) =>
        branch == nameof(SoundRequestEvent.PlayerStateChange)
            ? SoundRequestHubMapper.ToHubState((PlayerStateSnapshot)payload!)
            : payload;

    private Task DispatchAsync(
        SoundRequestEvent notification,
        CancellationToken cancellationToken
    ) =>
        OneofHubRelay.DispatchAsync<SoundRequestEvent, ISoundRequestHub>(
            notification,
            hubContext.Clients,
            methodNames: null,
            TransformPayload,
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
