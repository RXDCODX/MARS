using MARS.Alerts.Hubs.Interfaces;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Telegramus;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace MARS.Alerts.Hubs;

/// <summary>
/// Перекладывает события из broadcaster'а в методы хаба оверлея.
/// </summary>
/// <remarks>
/// <para>
/// Реле — единственный потребитель broadcaster'а на стороне браузера. Награды и
/// системные события по-прежнему пишут в <c>ITelegramusNotifier</c>, он по-прежнему
/// кладёт оболочку в очередь, а до клиента событие доходит уже по SignalR.
/// Благодаря этому 21 reward-хендлер не знает про транспорт и не менялся.
/// </para>
/// <para>
/// Имя метода на проводе берётся из <c>nameof</c> по
/// <c>ITelegramusHub</c>, а не пишется строкой. Типизированных методов у
/// <c>IHubContext</c> нет — <c>IHubClients&lt;T&gt;</c> существует только у
/// экземпляра хаба, а из фоновой задачи до него не дотянуться, — поэтому вызов
/// идёт через <c>SendCoreAsync</c>. Строка в этом вызове не анонимная:
/// переименование метода в интерфейсе ломает сборку.
/// </para>
/// <para>
/// Отправка идёт всем подписчикам хаба. Исключение отправителя
/// (<c>BroadcastExceptAsync</c>) работает на уровне broadcaster'а и сохраняется:
/// тот, кто вызвал событие сам, его не получает.
/// </para>
/// <para>
/// Реле читает медленнее gRPC-стрима: между чтением и отправкой стоит сеть.
/// Поэтому подписка с запасом — при стандартных 64 всплеск наград переполнил бы
/// очередь, и <c>DropWrite</c> молча отбросил бы события для всех клиентов.
/// </para>
/// </remarks>
public class HubEventRelay(
    GrpcEventBroadcaster<TelegramusEvent> broadcaster,
    IHubContext<OverlayHub> hubContext
) : BackgroundService
{
    /// <summary>
    /// Имя подписки реле. На поведение не влияет: имя нужно только для
    /// исключения отправителя из рассылки.
    /// </summary>
    public const string SubscriberId = "overlay";

    /// <summary>
    /// Ёмкость очереди реле. Больше стандартных 64: реле успевает отставать за
    /// всплеском наград, а очередь конечна.
    /// </summary>
    public const int SubscriptionCapacity = 512;

    /// <summary>
    /// Подписка, заведённая до запуска фоновой задачи. Присваивается один раз
    /// и освобождается в <see cref="StopAsync"/>.
    /// </summary>
    private GrpcSubscription<TelegramusEvent>? _subscription;

    /// <summary>
    /// Раскладывает оболочку по методам хаба.
    /// </summary>
    /// <remarks>
    /// Шесть методов не принимают аргументов: это ветки <c>oneof</c> с
    /// <c>EmptyEvent</c>, и клиенту нечего в них передавать. Им отправляется
    /// пустой список аргументов, а не пустой объект.
    /// <para>
    /// Ветка <c>None</c> означает сообщение без события — его может собрать
    /// клиент, отправляющий оболочку вручную. Отправлять некуда, и вызывать все
    /// методы подряд было бы заметным на глаз поведением, поэтому она пропускается.
    /// </para>
    /// </remarks>
    public async Task DispatchAsync(
        TelegramusEvent notification,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (notification.EventCase)
        {
            case TelegramusEvent.EventOneofCase.Alert:
                await SendAsync(
                    nameof(ITelegramusHub.Alert),
                    [notification.Alert],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.Alerts:
                await SendAsync(
                    nameof(ITelegramusHub.Alerts),
                    [notification.Alerts],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.WaifuRoll:
                await SendAsync(
                    nameof(ITelegramusHub.WaifuRoll),
                    [notification.WaifuRoll],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.AddNewWaifu:
                await SendAsync(
                    nameof(ITelegramusHub.AddNewWaifu),
                    [notification.AddNewWaifu],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.ShowCurrentWife:
                await SendAsync(
                    nameof(ITelegramusHub.ShowCurrentWife),
                    [notification.ShowCurrentWife],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.MergeWaifu:
                await SendAsync(
                    nameof(ITelegramusHub.MergeWaifu),
                    [notification.MergeWaifu],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.UpdateWaifuPrizes:
                await SendAsync(
                    nameof(ITelegramusHub.UpdateWaifuPrizes),
                    [notification.UpdateWaifuPrizes],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.FumoFriday:
                await SendAsync(
                    nameof(ITelegramusHub.FumoFriday),
                    [notification.FumoFriday],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.NewMessage:
                await SendAsync(
                    nameof(ITelegramusHub.NewMessage),
                    [notification.NewMessage],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.DeleteMessage:
                await SendAsync(
                    nameof(ITelegramusHub.DeleteMessage),
                    [notification.DeleteMessage],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.Highlite:
                await SendAsync(
                    nameof(ITelegramusHub.Highlite),
                    [notification.Highlite],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.PostTwitchInfo:
                await SendAsync(
                    nameof(ITelegramusHub.PostTwitchInfo),
                    [notification.PostTwitchInfo],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.MakeScreenParticles:
                await SendAsync(
                    nameof(ITelegramusHub.MakeScreenParticles),
                    [notification.MakeScreenParticles],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.MakeScreenEmojisParticles:
                await SendAsync(
                    nameof(ITelegramusHub.MakeScreenEmojisParticles),
                    [notification.MakeScreenEmojisParticles],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.RandomMem:
                await SendAsync(
                    nameof(ITelegramusHub.RandomMem),
                    [notification.RandomMem],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.AutoMessage:
                await SendAsync(
                    nameof(ITelegramusHub.AutoMessage),
                    [notification.AutoMessage],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.Adhd:
                await SendAsync(
                    nameof(ITelegramusHub.Adhd),
                    [notification.Adhd],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.Explosion:
                await SendAsync(nameof(ITelegramusHub.Explosion), [], cancellationToken);
                break;
            case TelegramusEvent.EventOneofCase.LeroyAlert:
                await SendAsync(nameof(ITelegramusHub.LeroyAlert), [], cancellationToken);
                break;
            case TelegramusEvent.EventOneofCase.GaoAlert:
                await SendAsync(
                    nameof(ITelegramusHub.GaoAlert),
                    [notification.GaoAlert],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.Credits:
                await SendAsync(nameof(ITelegramusHub.Credits), [], cancellationToken);
                break;
            case TelegramusEvent.EventOneofCase.MichaelJackson:
                await SendAsync(nameof(ITelegramusHub.MichaelJackson), [], cancellationToken);
                break;
            case TelegramusEvent.EventOneofCase.MikuMonday:
                await SendAsync(
                    nameof(ITelegramusHub.MikuMonday),
                    [notification.MikuMonday],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.MikuMikuBeam:
                await SendAsync(
                    nameof(ITelegramusHub.MikuMikuBeam),
                    [notification.MikuMikuBeam],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.PhonkEdit:
                await SendAsync(nameof(ITelegramusHub.PhonkEdit), [], cancellationToken);
                break;
            case TelegramusEvent.EventOneofCase.TikTokEdit:
                await SendAsync(
                    nameof(ITelegramusHub.TikTokEdit),
                    [notification.TikTokEdit],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.AllRefund:
                await SendAsync(
                    nameof(ITelegramusHub.AllRefund),
                    [notification.AllRefund],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.AudioQuizStart:
                await SendAsync(
                    nameof(ITelegramusHub.AudioQuizStart),
                    [notification.AudioQuizStart],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.AudioQuizStop:
                await SendAsync(nameof(ITelegramusHub.AudioQuizStop), [], cancellationToken);
                break;
            case TelegramusEvent.EventOneofCase.FumoRoll:
                await SendAsync(
                    nameof(ITelegramusHub.FumoRoll),
                    [notification.FumoRoll],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.UpdateFumoPrizes:
                await SendAsync(
                    nameof(ITelegramusHub.UpdateFumoPrizes),
                    [notification.UpdateFumoPrizes],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.FrogRoll:
                await SendAsync(
                    nameof(ITelegramusHub.FrogRoll),
                    [notification.FrogRoll],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.UpdateFrogPrizes:
                await SendAsync(
                    nameof(ITelegramusHub.UpdateFrogPrizes),
                    [notification.UpdateFrogPrizes],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.MikuRoll:
                await SendAsync(
                    nameof(ITelegramusHub.MikuRoll),
                    [notification.MikuRoll],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.UpdateMikuPrizes:
                await SendAsync(
                    nameof(ITelegramusHub.UpdateMikuPrizes),
                    [notification.UpdateMikuPrizes],
                    cancellationToken
                );
                break;
            case TelegramusEvent.EventOneofCase.AdhdConfig:
                await SendAsync(
                    nameof(ITelegramusHub.AdhdConfig),
                    [notification.AdhdConfig],
                    cancellationToken
                );
                break;
            default:
                break;
        }
    }

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
            // Остановка хоста: очередь закрыта вместе с токеном, читать больше нечего.
        }
    }

    /// <summary>
    /// Подписка заводится здесь, а не в теле фоновой задачи.
    /// </summary>
    /// <remarks>
    /// Начиная с .NET 8 <see cref="BackgroundService.StartAsync"/> не ждёт
    /// выполнения <c>ExecuteAsync</c>: задача стартует на пуле потоков и может
    /// не дойти до подписки до первого события. Событие, опубликованное сразу
    /// после старта хоста, уходило в пустоту, потому что подписчика ещё не было.
    /// На стенде Windows это не воспроизводилось, а на CI-раннере тест на
    /// <c>Background_loop_forwards_broadcast_event_to_hub</c> падал по таймауту.
    /// <para>
    /// Подписка должна существовать к моменту возврата <c>StartAsync</c>, а
    /// читает её уже фоновая задача. В <c>ExecuteAsync</c> остаётся запасной
    /// вызов на случай запуска без <c>StartAsync</c> — например, при прямом
    /// обращении в тестах.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Отправляет событие всем подписчикам хаба.
    /// </summary>
    private Task SendAsync(
        string method,
        object?[] arguments,
        CancellationToken cancellationToken
    ) => hubContext.Clients.All.SendCoreAsync(method, arguments, cancellationToken);
}
