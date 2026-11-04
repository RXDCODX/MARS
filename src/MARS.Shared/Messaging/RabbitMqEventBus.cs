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
    private readonly object _confirmLock = new();
    private readonly Queue<TaskCompletionSource<bool>> _pendingConfirms = new();
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
        if (_confirmEventsSubscribed || _channel is null)
        {
            return;
        }

        // Unroutable-сообщения (mandatory: true без подходящей очереди)
        // возвращаются через BasicReturnAsync, а не BasicNacksAsync.
        // По протоколу RabbitMQ: сначала Basic.Return, затем Basic.Ack.
        _channel.BasicReturnAsync += (sender, args) =>
        {
            logger.LogWarning(
                "Message unroutable: exchange={Exchange}, routingKey={RoutingKey}",
                args.Exchange,
                args.RoutingKey
            );

            // Возврат приходит в том же порядке, что и публикации — завершаем
            // самую старую ожидающую публикацию как неподтверждённую.
            lock (_confirmLock)
            {
                if (_pendingConfirms.TryDequeue(out var tcs))
                {
                    tcs.TrySetResult(false);
                }
            }

            MarsMetrics.RabbitMqUnroutable.Add(
                1,
                new KeyValuePair<string, object?>("routing_key", args.RoutingKey)
            );
            return Task.CompletedTask;
        };

        _channel.BasicAcksAsync += (sender, args) =>
        {
            logger.LogDebug(
                "Message acked: deliveryTag={DeliveryTag}, multiple={Multiple}",
                args.DeliveryTag,
                args.Multiple
            );

            lock (_confirmLock)
            {
                if (args.Multiple)
                {
                    while (_pendingConfirms.Count > 0)
                    {
                        var tcs = _pendingConfirms.Dequeue();
                        tcs.TrySetResult(true);
                    }
                }
                else
                {
                    if (_pendingConfirms.TryDequeue(out var tcs))
                    {
                        tcs.TrySetResult(true);
                    }
                }
            }

            return Task.CompletedTask;
        };

        _confirmEventsSubscribed = true;
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

        var confirmTcs = new TaskCompletionSource<bool>();

        lock (_confirmLock)
        {
            _pendingConfirms.Enqueue(confirmTcs);
        }

        await _channel!
            .BasicPublishAsync(
                exchange: RabbitMqConfig.ExchangeName,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: ct
            )
            .ConfigureAwait(false);

        // Дожидаемся подтверждения брокера (Publisher Confirms). При mandatory: true
        // и отсутствии подходящей очереди брокер сначала вызовет BasicReturnAsync,
        // затем BasicAcksAsync. Если сообщение неroutes, TCS завершится с false
        // на BasicReturnAsync, и метрика Published не увеличится.
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        var completedTask = await Task.WhenAny(confirmTcs.Task, Task.Delay(-1, linkedCts.Token));

        if (completedTask != confirmTcs.Task)
        {
            logger.LogError("Publish confirmation timed out for {RoutingKey}", routingKey);
            throw new TimeoutException($"Publish confirmation timed out for {routingKey}");
        }

        var acknowledged = await confirmTcs.Task;
        if (!acknowledged)
        {
            logger.LogWarning("Message not acknowledged by broker for {RoutingKey}", routingKey);
            throw new InvalidOperationException(
                $"Message to {routingKey} was not acknowledged by broker"
            );
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
            while (_pendingConfirms.Count > 0)
            {
                var tcs = _pendingConfirms.Dequeue();
                tcs.TrySetResult(false);
            }
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
