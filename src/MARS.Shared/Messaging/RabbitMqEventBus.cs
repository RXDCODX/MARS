using System.Text.Json;
using MARS.Shared.Configuration;
using MARS.Shared.Telemetry;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace MARS.Shared.Messaging;

/// <summary>
/// Продюсер событий RabbitMQ.
/// Канал публикации защищён семафором (каналы RabbitMQ.Client не потокобезопасны),
/// соединение ленивое и восстанавливается после обрыва.
/// </summary>
public class RabbitMqEventBus : IMarsEventBus, IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly string _serviceName;
    private readonly string _host;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly SemaphoreSlim _publishLock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqEventBus(
        RabbitMqOptions options,
        string serviceName,
        ILogger<RabbitMqEventBus> logger
    )
    {
        _logger = logger;
        _serviceName = serviceName;
        _host = options.Host;
        _factory = RabbitMqConnectionFactory.Create(options, $"mars-{serviceName}-publisher");
    }

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

            _logger.LogInformation(
                "RabbitMQ connected to {Host}, exchange={Exchange}",
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

        await _publishLock.WaitAsync(ct);

        try
        {
            await PublishCoreAsync(routingKey, body, ct);
        }
        catch (AlreadyClosedException)
        {
            _logger.LogWarning(
                "RabbitMQ channel closed while publishing {RoutingKey}, reconnecting",
                routingKey
            );

            await EnsureChannelAsync(ct);
            await PublishCoreAsync(routingKey, body, ct);
        }
        finally
        {
            _publishLock.Release();
        }
    }

    private async Task PublishCoreAsync(string routingKey, byte[] body, CancellationToken ct)
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

        properties.Headers["mars.source_service"] = _serviceName;
        properties.Headers["mars.timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        await _channel!
            .BasicPublishAsync(
                exchange: RabbitMqConfig.ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: ct
            )
            .ConfigureAwait(false);

        MarsMetrics.RabbitMqPublished.Add(
            1,
            new KeyValuePair<string, object?>("exchange", RabbitMqConfig.ExchangeName),
            new KeyValuePair<string, object?>("routing_key", routingKey)
        );

        _logger.LogDebug("Published to {RoutingKey}", routingKey);
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
