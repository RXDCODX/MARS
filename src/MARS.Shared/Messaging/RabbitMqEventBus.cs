using System.Text.Json;
using MARS.Shared.Configuration;
using MARS.Shared.Telemetry;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace MARS.Shared.Messaging;

/// <summary>
/// Продюсер событий RabbitMQ.
/// Канал публикации защищён семафором (каналы RabbitMQ.Client не потокобезопасны),
/// соединение ленивое и восстанавливается после обрыва. Включены Publisher
/// Confirms через <see cref="CreateChannelOptions"/> и mandatory: true —
/// публикация считается успешной только после подтверждения брокера,
/// а unroutable-сообщения обнаруживаются через <see cref="IChannel.BasicReturnAsync"/>.
/// </summary>
public class RabbitMqEventBus(
    RabbitMqOptions options,
    string serviceName,
    ILogger<RabbitMqEventBus> logger
) : IMarsEventBus, IAsyncDisposable
{
    private readonly ConnectionFactory _factory = RabbitMqConnectionFactory.Create(
        options,
        $"mars-{serviceName}-publisher"
    );
    private readonly string _host = options.Host;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly SemaphoreSlim _publishLock = new(1, 1);

    private sealed class PendingConfirm(string messageId)
    {
        public string MessageId { get; } = messageId;
        public bool WasReturned { get; set; }
        public TaskCompletionSource<bool> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private readonly object _confirmLock = new();
    private readonly SortedDictionary<ulong, PendingConfirm> _pendingConfirms = new();
    private readonly Dictionary<string, ulong> _sequencesByMessageId = new(StringComparer.Ordinal);
    private bool _confirmEventsSubscribed;

    private IConnection? _connection;
    private IChannel? _channel;

    private async Task EnsureChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
        {
            SubscribeToConfirmEvents();
            return;
        }

        await _initLock.WaitAsync(ct);

        try
        {
            if (_channel is { IsOpen: true })
            {
                SubscribeToConfirmEvents();
                return;
            }

            if (_connection is not { IsOpen: true })
            {
                await DisposeConnectionAsync();
                _connection = await _factory.CreateConnectionAsync(ct);
            }

            var channelOptions = new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true
            );

            _channel = await _connection.CreateChannelAsync(channelOptions, cancellationToken: ct);

            await _channel.ExchangeDeclareAsync(
                exchange: RabbitMqConfig.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct
            );

            SubscribeToConfirmEvents();

            logger.LogInformation(
                "RabbitMQ connected to {Host}, exchange={Exchange}, confirms enabled",
                _host,
                RabbitMqConfig.ExchangeName
            );
        }
        catch
        {
            await DisposeConnectionAsync();
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private void SubscribeToConfirmEvents()
    {
        var channel = _channel;
        if (_confirmEventsSubscribed || channel is null)
        {
            return;
        }

        channel.BasicReturnAsync += (sender, args) =>
        {
            logger.LogWarning(
                "Message unroutable: exchange={Exchange}, routingKey={RoutingKey}",
                args.Exchange,
                args.RoutingKey
            );

            var messageId = args.BasicProperties.MessageId;
            lock (_confirmLock)
            {
                if (
                    !string.IsNullOrWhiteSpace(messageId)
                    && _sequencesByMessageId.TryGetValue(messageId, out var sequence)
                    && _pendingConfirms.TryGetValue(sequence, out var pending)
                )
                {
                    // Basic.Return precedes the confirm for the same publication.
                    // Keep the entry until its ACK/NACK consumes this sequence.
                    pending.WasReturned = true;
                }
            }

            MarsMetrics.RabbitMqUnroutable.Add(
                1,
                new KeyValuePair<string, object?>("routing_key", args.RoutingKey)
            );
            return Task.CompletedTask;
        };

        channel.BasicAcksAsync += (sender, args) =>
        {
            logger.LogDebug(
                "Message acked: deliveryTag={DeliveryTag}, multiple={Multiple}",
                args.DeliveryTag,
                args.Multiple
            );

            CompleteConfirms(channel, args.DeliveryTag, args.Multiple, acknowledged: true);
            return Task.CompletedTask;
        };

        channel.BasicNacksAsync += (sender, args) =>
        {
            logger.LogWarning(
                "Message nacked: deliveryTag={DeliveryTag}, multiple={Multiple}",
                args.DeliveryTag,
                args.Multiple
            );

            CompleteConfirms(channel, args.DeliveryTag, args.Multiple, acknowledged: false);
            return Task.CompletedTask;
        };

        _confirmEventsSubscribed = true;
    }

    private void CompleteConfirms(
        IChannel sourceChannel,
        ulong deliveryTag,
        bool multiple,
        bool acknowledged
    )
    {
        lock (_confirmLock)
        {
            // A late callback from a replaced channel must never complete a
            // publication registered on the new channel.
            if (!ReferenceEquals(_channel, sourceChannel))
            {
                return;
            }

            var completed = _pendingConfirms
                .Where(entry =>
                    multiple
                        ? deliveryTag == 0 || entry.Key <= deliveryTag
                        : entry.Key == deliveryTag
                )
                .ToArray();

            foreach (var entry in completed)
            {
                _pendingConfirms.Remove(entry.Key);
                _sequencesByMessageId.Remove(entry.Value.MessageId);
                entry.Value.Completion.TrySetResult(acknowledged && !entry.Value.WasReturned);
            }
        }
    }

    public async Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var messageId = Guid.NewGuid().ToString();

        await _publishLock.WaitAsync(ct);

        try
        {
            await PublishCoreAsync(routingKey, body, messageId, ct);
        }
        catch (AlreadyClosedException)
        {
            logger.LogWarning(
                "RabbitMQ channel closed while publishing {RoutingKey}, reconnecting",
                routingKey
            );

            await EnsureChannelAsync(ct);
            await PublishCoreAsync(routingKey, body, messageId, ct);
        }
        finally
        {
            _publishLock.Release();
        }
    }

    private async Task PublishCoreAsync(
        string routingKey,
        byte[] body,
        string messageId,
        CancellationToken ct
    )
    {
        await EnsureChannelAsync(ct);

        using var activity = MarsActivities.StartRabbitMqPublish(
            RabbitMqConfig.ExchangeName,
            routingKey
        );

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = messageId,
            CorrelationId = messageId,
            Headers = new Dictionary<string, object?>(),
        };

        if (activity is not null)
        {
            properties.Headers["traceparent"] = activity.Id ?? "";
            if (activity.TraceStateString is not null)
            {
                properties.Headers["tracestate"] = activity.TraceStateString;
            }
        }

        properties.Headers["mars.source_service"] = serviceName;
        properties.Headers["mars.timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var channel =
            _channel ?? throw new InvalidOperationException("RabbitMQ channel is not initialized");
        var sequence = await channel.GetNextPublishSequenceNumberAsync(ct);
        var pending = new PendingConfirm(messageId);

        lock (_confirmLock)
        {
            _pendingConfirms.Add(sequence, pending);
            _sequencesByMessageId.Add(messageId, sequence);
        }

        try
        {
            await channel
                .BasicPublishAsync(
                    exchange: RabbitMqConfig.ExchangeName,
                    routingKey: routingKey,
                    mandatory: true,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: ct
                )
                .ConfigureAwait(false);

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                ct,
                timeoutCts.Token
            );

            bool acknowledged;
            try
            {
                acknowledged = await pending.Completion.Task.WaitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // The outcome is unknown: do not reuse this channel, because a
                // late broker confirm could otherwise be confused with a later publish.
                await DisposeConnectionAsync();
                throw;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                await DisposeConnectionAsync();
                throw new TimeoutException($"Publish confirmation timed out for {routingKey}");
            }

            if (!acknowledged)
            {
                logger.LogWarning(
                    "Message not acknowledged by broker for {RoutingKey}",
                    routingKey
                );
                throw new InvalidOperationException(
                    $"Message to {routingKey} was not acknowledged by broker"
                );
            }
        }
        catch (AlreadyClosedException)
        {
            lock (_confirmLock)
            {
                _pendingConfirms.Remove(sequence);
                _sequencesByMessageId.Remove(messageId);
            }

            _confirmEventsSubscribed = false;
            throw;
        }
        catch
        {
            // BasicPublishAsync can fail after the broker has received the frame.
            // If there is no final confirm yet, discard the channel before retrying.
            if (!pending.Completion.Task.IsCompleted && ReferenceEquals(_channel, channel))
            {
                await DisposeConnectionAsync();
            }

            throw;
        }

        MarsMetrics.RabbitMqPublished.Add(
            1,
            new KeyValuePair<string, object?>("exchange", RabbitMqConfig.ExchangeName),
            new KeyValuePair<string, object?>("routing_key", routingKey)
        );

        logger.LogDebug("Published to {RoutingKey}", routingKey);
    }

    private async Task DisposeConnectionAsync()
    {
        lock (_confirmLock)
        {
            foreach (var pending in _pendingConfirms.Values)
            {
                pending.Completion.TrySetResult(false);
            }

            _pendingConfirms.Clear();
            _sequencesByMessageId.Clear();
        }

        _confirmEventsSubscribed = false;

        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeConnectionAsync();
        _initLock.Dispose();
        _publishLock.Dispose();
    }
}
