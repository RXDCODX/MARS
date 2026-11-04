using System.Text;
using System.Text.Json;
using MARS.Shared.Configuration;
using MARS.Shared.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace MARS.Shared.Messaging;

/// <summary>
/// Базовый класс потребителей RabbitMQ: объявляет exchange/queue, биндит routing keys,
/// переподключается при обрыве брокера и после исчерпания попыток отправляет
/// «отравленное» сообщение в dead-letter-очередь вместо бесконечного requeue.
/// Включены Publisher Confirms через <see cref="CreateChannelOptions"/>:
/// retry/DLQ публикуются только в исходную очередь этого потребителя, и исходная
/// доставка подтверждается только после успешного переноса сообщения.
/// </summary>
public abstract class RabbitMqConsumerBase : BackgroundService, IAsyncDisposable
{
    internal const string RetryHeader = "mars.retry-count";

    private readonly ConnectionFactory _factory;
    private readonly ILogger _logger;
    private readonly string _serviceName;
    private readonly string _queueName;
    private readonly string[] _routingKeys;
    private readonly int _maxDeliveryAttempts;
    private readonly TimeSpan _reconnectDelay;
    private readonly string _deadLetterQueueName;

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

    protected RabbitMqConsumerBase(
        RabbitMqOptions options,
        string serviceName,
        string queueName,
        string[] routingKeys,
        ILogger logger
    )
    {
        _serviceName = serviceName;
        _queueName = queueName;
        _routingKeys = routingKeys;
        _logger = logger;
        _maxDeliveryAttempts = Math.Max(1, options.MaxDeliveryAttempts);
        _reconnectDelay = TimeSpan.FromMilliseconds(
            Math.Max(500, options.ReconnectDelayMilliseconds)
        );
        _factory = RabbitMqConnectionFactory.Create(options, $"mars-{serviceName}-{queueName}");
        _deadLetterQueueName = $"{queueName}.dlq";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeLoopAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                MarsMetrics.RabbitMqConsumerErrors.Add(
                    1,
                    new KeyValuePair<string, object?>("queue", _queueName)
                );

                _logger.LogError(
                    ex,
                    "RabbitMQ consumer {Queue} lost connection, reconnecting in {Delay}",
                    _queueName,
                    _reconnectDelay
                );

                await DisposeConnectionAsync();

                try
                {
                    await Task.Delay(_reconnectDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    private async Task ConsumeLoopAsync(CancellationToken stoppingToken)
    {
        _connection = await _factory.CreateConnectionAsync(stoppingToken);

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
        );

        _channel = await _connection.CreateChannelAsync(
            channelOptions,
            cancellationToken: stoppingToken
        );

        await _channel.ExchangeDeclareAsync(
            exchange: RabbitMqConfig.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        await _channel.QueueDeclareAsync(
            queue: _deadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        await _channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        foreach (var routingKey in _routingKeys)
        {
            await _channel.QueueBindAsync(
                queue: _queueName,
                exchange: RabbitMqConfig.ExchangeName,
                routingKey: routingKey,
                arguments: null,
                cancellationToken: stoppingToken
            );
        }

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken
        );

        // Подтверждения публикации retry/DLQ отслеживаются через эти события.
        SubscribeToConfirmEvents();

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += HandleDeliveryAsync;

        await _channel.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        _logger.LogInformation(
            "RabbitMQ consumer started: service={Service}, queue={Queue}, keys=[{Keys}]",
            _serviceName,
            _queueName,
            string.Join(", ", _routingKeys)
        );

        await WaitUntilChannelClosesAsync(stoppingToken);
    }

    private void FailPendingConfirms()
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
    }

    private void SubscribeToConfirmEvents()
    {
        var channel = _channel;
        if (_confirmEventsSubscribed || channel is null)
        {
            return;
        }

        channel.BasicAcksAsync += (sender, args) =>
        {
            CompleteConfirms(channel, args.DeliveryTag, args.Multiple, acknowledged: true);
            return Task.CompletedTask;
        };

        channel.BasicNacksAsync += (sender, args) =>
        {
            CompleteConfirms(channel, args.DeliveryTag, args.Multiple, acknowledged: false);
            return Task.CompletedTask;
        };

        channel.BasicReturnAsync += (sender, args) =>
        {
            _logger.LogWarning(
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
                    // Keep the confirm pending: RabbitMQ sends ACK after Return.
                    pending.WasReturned = true;
                }
            }

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

    private async Task WaitUntilChannelClosesAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_channel is null || !_channel.IsOpen)
            {
                throw new InvalidOperationException($"RabbitMQ channel for {_queueName} is closed");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        stoppingToken.ThrowIfCancellationRequested();
    }

    private async Task HandleDeliveryAsync(object sender, BasicDeliverEventArgs ea)
    {
        var channel = _channel;

        if (channel is null)
        {
            return;
        }

        // Гарантируем подписку на события подтверждений публикации (нужно для
        // тестов, где канал подставляется напрямую, минуя ConsumeLoopAsync).
        SubscribeToConfirmEvents();

        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);

            _logger.LogDebug("Received message on {RoutingKey}", ea.RoutingKey);

            await HandleMessageAsync(ea.RoutingKey, json, CancellationToken.None);

            await channel.BasicAckAsync(
                ea.DeliveryTag,
                multiple: false,
                cancellationToken: CancellationToken.None
            );

            MarsMetrics.RabbitMqConsumed.Add(
                1,
                new KeyValuePair<string, object?>("queue", _queueName)
            );
        }
        catch (Exception ex)
        {
            var attempt = ReadRetryCount(ea.BasicProperties) + 1;
            var exhausted = attempt >= _maxDeliveryAttempts;

            _logger.LogError(
                ex,
                "Error processing message from {Queue} (attempt {Attempt}/{Max}){DeadLetter}",
                _queueName,
                attempt,
                _maxDeliveryAttempts,
                exhausted ? ", sending to dead-letter" : string.Empty
            );

            MarsMetrics.RabbitMqConsumerErrors.Add(
                1,
                new KeyValuePair<string, object?>("queue", _queueName)
            );

            try
            {
                if (exhausted)
                {
                    await SendToDeadLetterAsync(channel, ea);
                }
                else
                {
                    await RepublishWithRetryAsync(channel, ea, attempt);
                }

                // Подтверждаем исходную доставку только после успешного переноса
                // сообщения в retry/DLQ.
                await channel.BasicNackAsync(
                    ea.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: CancellationToken.None
                );
            }
            catch (Exception settleException)
            {
                // Перенос в retry/DLQ не удался: сообщение остаётся в очереди.
                // Закрываем канал, чтобы ExecuteAsync переподключился и RabbitMQ
                // вернул unacked сообщение обратно в очередь (at-least-once с
                // дублями, обработчики должны быть идемпотентными).
                _logger.LogWarning(
                    settleException,
                    "Failed to settle delivery on {Queue}, closing channel to redeliver message",
                    _queueName
                );

                FailPendingConfirms();

                try
                {
                    await channel.CloseAsync(500, "Settle failed", false, CancellationToken.None);
                }
                catch
                {
                    // Игнорируем ошибки закрытия — ExecuteAsync уже поднимет флаг
                    // закрытия канала и переподключится.
                }

                await DisposeConnectionAsync();

                // Выбрасываем, чтобы ConsumeLoopAsync выбросил исключение выше,
                // ExecuteAsync поймает его и переподключится.
                throw;
            }
        }
    }

    private async Task RepublishWithRetryAsync(
        IChannel channel,
        BasicDeliverEventArgs ea,
        int attempt
    )
    {
        // Retry is sent only to this consumer's queue through the default exchange.
        await PublishAndConfirmAsync(
            channel,
            exchange: string.Empty,
            routingKey: _queueName,
            properties: BuildRetryProperties(ea.BasicProperties, attempt),
            body: ea.Body,
            description: $"Retry publish to {_queueName}"
        );
    }

    private async Task SendToDeadLetterAsync(IChannel channel, BasicDeliverEventArgs ea)
    {
        await PublishAndConfirmAsync(
            channel,
            exchange: string.Empty,
            routingKey: _deadLetterQueueName,
            properties: BuildDeadLetterProperties(ea.BasicProperties),
            body: ea.Body,
            description: $"Dead-letter publish to {_deadLetterQueueName}"
        );
    }

    private async Task PublishAndConfirmAsync(
        IChannel channel,
        string exchange,
        string routingKey,
        BasicProperties properties,
        ReadOnlyMemory<byte> body,
        string description
    )
    {
        // Use a per-transfer MessageId for Basic.Return correlation while keeping
        // CorrelationId linked to the original event across retry/DLQ.
        properties.CorrelationId ??= properties.MessageId;
        var messageId = Guid.NewGuid().ToString();
        properties.MessageId = messageId;

        var sequence = await channel.GetNextPublishSequenceNumberAsync(CancellationToken.None);
        var pending = new PendingConfirm(messageId);

        lock (_confirmLock)
        {
            _pendingConfirms.Add(sequence, pending);
            _sequencesByMessageId.Add(messageId, sequence);
        }

        try
        {
            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: CancellationToken.None
            );

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            bool acknowledged;

            try
            {
                acknowledged = await pending.Completion.Task.WaitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                throw new TimeoutException($"{description} timed out");
            }

            if (!acknowledged)
            {
                throw new InvalidOperationException(
                    $"{description} was not acknowledged by broker"
                );
            }
        }
        finally
        {
            // If publish timed out or threw, the outer delivery handler closes
            // this channel. Sequence-based correlation makes late confirms harmless.
            lock (_confirmLock)
            {
                _pendingConfirms.Remove(sequence);
                _sequencesByMessageId.Remove(messageId);
            }
        }
    }

    private static BasicProperties BuildRetryProperties(
        IReadOnlyBasicProperties source,
        int attempt
    )
    {
        var properties = CopyProperties(source);
        properties.Headers![RabbitMqConsumerBase.RetryHeader] = attempt;

        return properties;
    }

    private static BasicProperties BuildDeadLetterProperties(IReadOnlyBasicProperties source)
    {
        var properties = CopyProperties(source);
        var reason = properties.Headers!.TryGetValue(RetryHeader, out var raw) ? raw : "unknown";
        properties.Headers["mars.dead_letter.reason"] = reason;
        properties.Headers.Remove(RetryHeader);

        return properties;
    }

    private static BasicProperties CopyProperties(IReadOnlyBasicProperties source)
    {
        var properties = new BasicProperties
        {
            ContentType = source.ContentType,
            ContentEncoding = source.ContentEncoding,
            DeliveryMode = source.DeliveryMode,
            Priority = source.Priority,
            CorrelationId = source.CorrelationId,
            ReplyTo = source.ReplyTo,
            Expiration = source.Expiration,
            MessageId = source.MessageId,
            Type = source.Type,
            UserId = source.UserId,
            AppId = source.AppId,
            Headers = source.Headers is null
                ? new Dictionary<string, object?>()
                : new Dictionary<string, object?>(source.Headers),
        };

        return properties;
    }

    private static int ReadRetryCount(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers is null || !properties.Headers.TryGetValue(RetryHeader, out var raw))
        {
            return 0;
        }

        return raw switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            byte byteValue => byteValue,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => 0,
        };
    }

    protected abstract Task HandleMessageAsync(
        string routingKey,
        string json,
        CancellationToken ct
    );

    protected static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
    }

    private async Task DisposeConnectionAsync()
    {
        FailPendingConfirms();
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

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await DisposeConnectionAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeConnectionAsync();
    }
}
