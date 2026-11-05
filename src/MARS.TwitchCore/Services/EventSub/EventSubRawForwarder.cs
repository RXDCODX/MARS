using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MARS.Shared.Messaging;
using MARS.Shared.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using Wolverine;

namespace MARS.TwitchCore.Services.EventSub;

public sealed class EventSubRawForwarder(
    EventSubWebsocketClient wsClient,
    IMessageBus messageBus,
    ILogger<EventSubRawForwarder> logger
) : IHostedService, IAsyncDisposable
{
    private readonly Channel<byte[]> _channel = Channel.CreateBounded<byte[]>(
        new BoundedChannelOptions(1024)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        }
    );

    private EventInfo? _onDataReceivedEvent;
    private object? _websocketClient;
    private EventHandler? _handler;
    private CancellationTokenSource? _cts;
    private Task? _workerTask;
    private bool _subscribed;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _workerTask = Task.Run(() => ProcessQueueAsync(_cts.Token), _cts.Token);
        TrySubscribe();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        TryUnsubscribe();
        if (_cts != null)
        {
            try
            {
                _cts.Cancel();
            }
            catch
            {
                // ignore
            }
        }
        _channel.Writer.TryComplete();

        if (_workerTask != null)
        {
            try
            {
                await _workerTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch
            {
                // ignore
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        TryUnsubscribe();
        if (_cts != null)
        {
            _cts.Dispose();
            _cts = null;
        }
        _channel.Writer.TryComplete();
        if (_workerTask != null)
        {
            try
            {
                await _workerTask;
            }
            catch
            {
                // ignore
            }
            _workerTask = null;
        }
    }

    private void TrySubscribe()
    {
        if (_subscribed)
        {
            return;
        }

        try
        {
            var wsClientType = wsClient.GetType();
            var field = wsClientType.GetField(
                "_websocketClient",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            if (field == null)
            {
                logger.LogWarning(
                    "EventSubRawForwarder: не удалось найти поле _websocketClient у {Type}",
                    wsClientType.FullName
                );
                return;
            }

            _websocketClient = field.GetValue(wsClient);
            if (_websocketClient == null)
            {
                logger.LogWarning("EventSubRawForwarder: _websocketClient is null");
                return;
            }

            var wsType = _websocketClient.GetType();
            _onDataReceivedEvent = wsType.GetEvent(
                "OnDataReceived",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            if (_onDataReceivedEvent == null)
            {
                logger.LogWarning(
                    "EventSubRawForwarder: не удалось найти событие OnDataReceived у {Type}",
                    wsType.FullName
                );
                return;
            }

            _handler = OnDataReceivedHandler;
            _onDataReceivedEvent.AddEventHandler(_websocketClient, _handler);
            _subscribed = true;
            logger.LogInformation("EventSubRawForwarder: подписка на сырой фрейм установлена");
        }
        catch (Exception ex)
        {
            MarsMetrics.EventSubRawErrors.Add(1);
            logger.LogError(ex, "EventSubRawForwarder: ошибка при подписке на сырой фрейм");
        }
    }

    private void TryUnsubscribe()
    {
        if (
            !_subscribed
            || _websocketClient == null
            || _onDataReceivedEvent == null
            || _handler == null
        )
        {
            return;
        }

        try
        {
            _onDataReceivedEvent.RemoveEventHandler(_websocketClient, _handler);
        }
        catch
        {
            // ignore
        }

        _subscribed = false;
    }

    private void OnDataReceivedHandler(object? sender, EventArgs e)
    {
        try
        {
            if (e == null)
            {
                return;
            }

            var dataArgsType = e.GetType();
            var bytesProp = dataArgsType.GetProperty(
                "Bytes",
                BindingFlags.Instance | BindingFlags.Public
            );
            if (bytesProp == null)
            {
                return;
            }

            if (bytesProp.GetValue(e) is not byte[] bytes || bytes.Length == 0)
            {
                return;
            }

            MarsMetrics.EventSubRawForwarded.Add(1);

            var copy = new byte[bytes.Length];
            Buffer.BlockCopy(bytes, 0, copy, 0, bytes.Length);

            if (!_channel.Writer.TryWrite(copy))
            {
                MarsMetrics.EventSubRawDropped.Add(1);
            }
        }
        catch (Exception ex)
        {
            MarsMetrics.EventSubRawErrors.Add(1);
            logger.LogDebug(ex, "EventSubRawForwarder: ошибка в обработчике сырых фреймов");
        }
    }

    private async Task ProcessQueueAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var bytes in _channel.Reader.ReadAllAsync(ct))
            {
                try
                {
                    await PublishRawFrameAsync(bytes, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    MarsMetrics.EventSubRawErrors.Add(1);
                    logger.LogDebug(ex, "EventSubRawForwarder: ошибка публикации сырых фреймов");
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // graceful
        }
        catch (Exception ex)
        {
            MarsMetrics.EventSubRawErrors.Add(1);
            logger.LogError(ex, "EventSubRawForwarder: фоновый воркер завершился с ошибкой");
        }
    }

    private async Task PublishRawFrameAsync(byte[] bytes, CancellationToken ct)
    {
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;

        string messageType = "unknown";
        if (root.TryGetProperty("metadata", out var metadataEl))
        {
            if (metadataEl.TryGetProperty("message_type", out var mt))
            {
                messageType = mt.GetString() ?? "unknown";
            }
        }

        string? subscriptionType = null;
        string? subscriptionVersion = null;
        string? messageId = null;

        if (root.TryGetProperty("metadata", out var metadataEl2))
        {
            if (metadataEl2.TryGetProperty("message_id", out var mid))
            {
                messageId = mid.GetString();
            }
            if (metadataEl2.TryGetProperty("subscription_type", out var st))
            {
                subscriptionType = st.GetString();
            }
            if (metadataEl2.TryGetProperty("subscription_version", out var sv))
            {
                subscriptionVersion = sv.GetString();
            }
        }

        string? subscriptionId = null;
        if (root.TryGetProperty("payload", out var payloadEl))
        {
            if (
                payloadEl.TryGetProperty("subscription", out var subEl)
                && subEl.ValueKind == JsonValueKind.Object
                && subEl.TryGetProperty("id", out var sid)
            )
            {
                subscriptionId = sid.GetString();
            }
        }

        var rawEvent = new EventSubRawEvent
        {
            SchemaVersion = 1,
            MessageId = messageId,
            MessageType = messageType,
            SubscriptionType = subscriptionType,
            SubscriptionVersion = subscriptionVersion,
            SubscriptionId = subscriptionId,
            SessionId = wsClient.SessionId,
            EventName = subscriptionType ?? messageType,
            ObservedAtUtc = DateTimeOffset.UtcNow,
            SourceService = "MARS.TwitchCore",
            TraceParent = Activity.Current?.Id,
            Raw = JsonSerializer.Deserialize<JsonElement>(bytes),
        };

        await messageBus.PublishAsync(rawEvent);
        MarsMetrics.EventSubRawPublished.Add(1);
    }
}
