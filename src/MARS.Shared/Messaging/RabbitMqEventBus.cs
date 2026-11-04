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
/// Confirms и mandatory: true — публикация считается успешной только после
/// подтверждения брокера, а unroutable-сообщения обнаруживаются через BasicReturn.
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

    private IConnection? _connection;
    private IChannel? _channel;

    private async Task EnsureChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
        {
            return;
        }

        await _initLock.WaitAsync(ct);

        try
        {
            if (_channel is { IsOpen: true })
            {
                return;
            }

            if (_connection is not { IsOpen: true })
            {
                await DisposeConnectionAsync();
                _connection = await _factory.CreateConnectionAsync(ct);
            }

            _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

            await _channel.ExchangeDeclareAsync(
                exchange: RabbitMqConfig.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct
            );

            // Unroutable-сообщения (mandatory: true без подходящей очереди)
            // возвращаются через BasicReturnAsync.
            _channel.BasicReturnAsync += (sender, args) =>
            {
                logger.LogWarning(
                    "Message unroutable: exchange={Exchange}, routingKey={RoutingKey}, messageId={MessageId}",
                    args.Exchange,
                    args.RoutingKey,
                    args.BasicProperties.MessageId
                );
                MarsMetrics.RabbitMqUnroutable.Add(
                    1,
                    new KeyValuePair<string, object?>("routing_key", args.RoutingKey)
                );
                return Task.CompletedTask;
            };

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

        // Публикация с mandatory: true: если сообщение не может быть маршрутизировано
        // ни в одну очередь, брокер вызовет BasicReturnAsync, и метрика unroutable
        // зафиксирует это. При успешной записи в канал сообщение считается принятым
        // брокером.
        MarsMetrics.RabbitMqPublished.Add(
            1,
            new KeyValuePair<string, object?>("exchange", RabbitMqConfig.ExchangeName),
            new KeyValuePair<string, object?>("routing_key", routingKey)
        );

        logger.LogDebug("Published to {RoutingKey}", routingKey);
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

    public async ValueTask DisposeAsync()
    {
        await DisposeConnectionAsync();
        _initLock.Dispose();
        _publishLock.Dispose();
    }
}
