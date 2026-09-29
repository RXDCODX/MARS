using System.Text;
using System.Text.Json;
using MARS.Shared.Configuration;
using MARS.Shared.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MARS.Shared.Messaging;

/// <summary>
/// Базовый класс потребителей RabbitMQ: объявляет exchange/queue, биндит routing keys,
/// переподключается при обрыве брокера и после исчерпания попыток отправляет
/// «отравленное» сообщение в dead-letter-очередь вместо бесконечного requeue.
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
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

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

                await channel.BasicNackAsync(
                    ea.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: CancellationToken.None
                );
            }
            catch (Exception settleException)
            {
                _logger.LogWarning(
                    settleException,
                    "Failed to settle delivery on {Queue}",
                    _queueName
                );
            }
        }
    }

    private async Task RepublishWithRetryAsync(
        IChannel channel,
        BasicDeliverEventArgs ea,
        int attempt
    )
    {
        await channel.BasicPublishAsync(
            exchange: RabbitMqConfig.ExchangeName,
            routingKey: ea.RoutingKey,
            mandatory: false,
            basicProperties: BuildRetryProperties(ea.BasicProperties, attempt),
            body: ea.Body,
            cancellationToken: CancellationToken.None
        );
    }

    private async Task SendToDeadLetterAsync(IChannel channel, BasicDeliverEventArgs ea)
    {
        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _deadLetterQueueName,
            mandatory: false,
            basicProperties: BuildDeadLetterProperties(ea.BasicProperties),
            body: ea.Body,
            cancellationToken: CancellationToken.None
        );
    }

    private static BasicProperties BuildRetryProperties(
        IReadOnlyBasicProperties source,
        int attempt
    )
    {
        var properties = CopyProperties(source);
        properties.Headers![RetryHeader] = attempt;

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
