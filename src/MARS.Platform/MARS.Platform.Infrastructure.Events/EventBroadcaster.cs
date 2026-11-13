using System.Collections.Concurrent;
using System.Threading.Channels;

namespace MARS.Platform.Infrastructure.Events;

/// <summary>
/// Широковещатель событий внутри процесса: каждый подписчик получает свою
/// очередь, а публикация не ждёт ни одного из них.
/// </summary>
/// <remarks>
/// Транспорта здесь нет намеренно. Раньше тип лежал в gRPC-проекте и требовал
/// <c>IMessage&lt;T&gt;</c> только потому, что умел перекладывать сообщения в
/// серверный поток gRPC. Поток переехал в расширение gRPC-проекта, а
/// широковещатель остался нужен и без gRPC: реле SignalR-хабов читают те же
/// очереди, а сервис-владелец оверлея публикует события вообще без транспорта.
/// </remarks>
public sealed class EventBroadcaster<TMessage>
    where TMessage : class
{
    private const int DefaultQueueCapacity = 64;

    private readonly ConcurrentDictionary<string, Channel<TMessage>> _subscribers = new(
        StringComparer.Ordinal
    );

    private readonly int _queueCapacity;

    public EventBroadcaster()
        : this(DefaultQueueCapacity) { }

    internal EventBroadcaster(int queueCapacity)
    {
        _queueCapacity = queueCapacity > 0 ? queueCapacity : DefaultQueueCapacity;
    }

    public int SubscriberCount => _subscribers.Count;

    public EventSubscription<TMessage> Subscribe()
    {
        // Через поле, а не через DefaultQueueCapacity: у broadcaster'а может быть
        // своя ёмкость (её задаёт внутренний конструктор для тестов), и подписка
        // обязана её уважать.
        return Subscribe(null, _queueCapacity);
    }

    /// <summary>
    /// Подписка с именем и собственной ёмкостью очереди.
    /// </summary>
    /// <param name="subscriberId">Имя подписчика; нужно, чтобы исключить отправителя.</param>
    /// <param name="capacity">
    /// Сколько сообщений подписчик может не забрать, прежде чем начнут теряться.
    /// Значение по умолчанию рассчитано на читателя, который забирает сообщение
    /// сразу и окупается одной записью на канал. Реле оверлейного хаба читает
    /// медленнее — оно ждёт сетевую отправку всем клиентам, — поэтому стандартных
    /// 64 ему не хватает: при всплеске наград очередь переполнялась бы и
    /// <c>DropWrite</c> отбрасывал бы события для всех клиентов, а не только для
    /// отставшего.
    /// </param>
    /// <remarks>
    /// Ёмкость нельзя сменить после подписки: канал создаётся вместе с ней.
    /// Потому она и передаётся сюда, а не настраивается на самом broadcasters.
    /// </remarks>
    public EventSubscription<TMessage> Subscribe(string? subscriberId, int capacity)
    {
        var id =
            !string.IsNullOrWhiteSpace(subscriberId) && !_subscribers.ContainsKey(subscriberId)
                ? subscriberId
                : Guid.NewGuid().ToString("N");

        var queue = Channel.CreateBounded<TMessage>(
            new BoundedChannelOptions(capacity > 0 ? capacity : _queueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropWrite,
            }
        );

        _subscribers[id] = queue;

        return new EventSubscription<TMessage>(this, id, queue);
    }

    /// <summary>
    /// Подписка с именем, которое задал клиент. Имя нужно, чтобы исключить
    /// отправителя из рассылки: <see cref="BroadcastExceptAsync"/> отбрасывает
    /// сообщение подписчику с совпадающим идентификатором, а узнать сгенерированный
    /// сервером id клиент не может.
    /// </summary>
    /// <remarks>
    /// Занятое имя игнорируется и подписчик получает сгенерированный id: два
    /// оверлея с одинаковым именем иначе молча вытеснили бы друг друга из
    /// словаря, и один перестал бы получать события без всякой ошибки.
    /// </remarks>
    public EventSubscription<TMessage> Subscribe(string? subscriberId)
    {
        return Subscribe(subscriberId, _queueCapacity);
    }

    public Task BroadcastAsync(TMessage message)
    {
        foreach (var queue in _subscribers.Values)
        {
            queue.Writer.TryWrite(message);
        }

        return Task.CompletedTask;
    }

    public Task BroadcastExceptAsync(string? subscriberId, TMessage message)
    {
        var excludedId = subscriberId ?? string.Empty;

        foreach (var (id, queue) in _subscribers)
        {
            if (!string.Equals(id, excludedId, StringComparison.Ordinal))
            {
                queue.Writer.TryWrite(message);
            }
        }

        return Task.CompletedTask;
    }

    public bool TryEnqueue(EventSubscription<TMessage> subscription, TMessage message)
    {
        var enqueued = _subscribers.ContainsKey(subscription.Id);

        if (enqueued)
        {
            subscription.Queue.Writer.TryWrite(message);
        }

        return enqueued;
    }

    internal void Unsubscribe(string id)
    {
        if (_subscribers.TryRemove(id, out var queue))
        {
            queue.Writer.TryComplete();
        }
    }
}
