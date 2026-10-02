using System.Collections.Concurrent;
using System.Threading.Channels;
using Google.Protobuf;
using Grpc.Core;

namespace MARS.Shared.Grpc;

public sealed class GrpcEventBroadcaster<TMessage>
    where TMessage : class, IMessage<TMessage>, new()
{
    private const int DefaultQueueCapacity = 64;

    private readonly ConcurrentDictionary<string, Channel<TMessage>> _subscribers = new(
        StringComparer.Ordinal
    );

    private readonly int _queueCapacity;

    public GrpcEventBroadcaster()
        : this(DefaultQueueCapacity) { }

    internal GrpcEventBroadcaster(int queueCapacity)
    {
        _queueCapacity = queueCapacity > 0 ? queueCapacity : DefaultQueueCapacity;
    }

    public int SubscriberCount => _subscribers.Count;

    public GrpcSubscription<TMessage> Subscribe()
    {
        return Subscribe(null);
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
    public GrpcSubscription<TMessage> Subscribe(string? subscriberId)
    {
        var id =
            !string.IsNullOrWhiteSpace(subscriberId) && !_subscribers.ContainsKey(subscriberId)
                ? subscriberId
                : Guid.NewGuid().ToString("N");

        var queue = Channel.CreateBounded<TMessage>(
            new BoundedChannelOptions(_queueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropWrite,
            }
        );

        _subscribers[id] = queue;

        return new GrpcSubscription<TMessage>(this, id, queue);
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

    public bool TryEnqueue(GrpcSubscription<TMessage> subscription, TMessage message)
    {
        var enqueued = _subscribers.ContainsKey(subscription.Id);

        if (enqueued)
        {
            subscription.Queue.Writer.TryWrite(message);
        }

        return enqueued;
    }

    public async Task<bool> PumpAsync(
        GrpcSubscription<TMessage> subscription,
        IAsyncStreamWriter<TMessage> writer,
        CancellationToken cancellationToken
    )
    {
        var stopped = false;

        while (!stopped && !cancellationToken.IsCancellationRequested)
        {
            stopped = await TryForwardNextAsync(subscription, writer, cancellationToken);
        }

        return stopped;
    }

    internal void Unsubscribe(string id)
    {
        if (_subscribers.TryRemove(id, out var queue))
        {
            queue.Writer.TryComplete();
        }
    }

    private static async Task<bool> TryForwardNextAsync(
        GrpcSubscription<TMessage> subscription,
        IAsyncStreamWriter<TMessage> writer,
        CancellationToken cancellationToken
    )
    {
        var stopped = false;

        try
        {
            var message = await subscription.Queue.Reader.ReadAsync(cancellationToken);
            await writer.WriteAsync(message);
        }
        catch (OperationCanceledException)
        {
            stopped = true;
        }
        catch (ChannelClosedException)
        {
            stopped = true;
        }

        return stopped;
    }
}
