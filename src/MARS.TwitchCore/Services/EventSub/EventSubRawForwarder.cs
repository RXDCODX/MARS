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

namespace MARS.TwitchCore.Services.EventSub;

/// <summary>
/// Copies raw Twitch EventSub WebSocket frames into the shared RabbitMQ event
/// exchange. The existing typed TwitchLib handlers continue to receive the same
/// frames; this service only adds a second, non-blocking observer.
/// </summary>
public sealed class EventSubRawForwarder(
    EventSubWebsocketClient wsClient,
    IMarsEventBus eventBus,
    ILogger<EventSubRawForwarder> logger
) : IHostedService, IAsyncDisposable
{
    private const string RoutingKey = "twitch.eventsub.raw";
    private const int BufferCapacity = 1024;

    private readonly Channel<PendingFrame> _channel = Channel.CreateBounded<PendingFrame>(
        new BoundedChannelOptions(BufferCapacity)
        {
            // Wait mode makes TryWrite return false when full. The callback never
            // waits, so this is an explicit, measurable drop-newest policy.
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        }
    );

    private EventInfo? _onDataReceivedEvent;
    private PropertyInfo? _bytesProperty;
    private object? _websocketClient;
    private Delegate? _handler;
    private CancellationTokenSource? _cts;
    private Task? _workerTask;
    private bool _subscribed;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_workerTask is not null)
        {
            return Task.CompletedTask;
        }

        _cts = new CancellationTokenSource();
        _workerTask = Task.Run(() => ProcessQueueAsync(_cts.Token), _cts.Token);
        TrySubscribe();

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        TryUnsubscribe();
        _channel.Writer.TryComplete();

        if (_workerTask is null)
        {
            return;
        }

        try
        {
            // Drain frames already accepted by the bounded buffer during normal
            // shutdown. If RabbitMQ is unavailable, stop waiting after five seconds.
            await _workerTask
                .WaitAsync(TimeSpan.FromSeconds(5), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            _cts?.Cancel();
            await ObserveWorkerAsync(_workerTask).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _cts?.Cancel();
            await ObserveWorkerAsync(_workerTask).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        TryUnsubscribe();
        _channel.Writer.TryComplete();
        _cts?.Cancel();

        if (_workerTask is not null)
        {
            await ObserveWorkerAsync(_workerTask).ConfigureAwait(false);
            _workerTask = null;
        }

        _cts?.Dispose();
        _cts = null;
    }

    private void TrySubscribe()
    {
        if (_subscribed)
        {
            return;
        }

        try
        {
            // TwitchLib.EventSub.Websockets 0.8.0 creates WebsocketClient inside
            // EventSubWebsocketClient and exposes its raw callback only as an
            // internal AsyncEventHandler<DataReceivedArgs> event. Keep reflection
            // limited to this field and event; re-check when upgrading TwitchLib.
            var wsClientType = wsClient.GetType();
            var field = wsClientType.GetField(
                "_websocketClient",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            if (field is null)
            {
                throw new MissingFieldException(wsClientType.FullName, "_websocketClient");
            }

            _websocketClient = field.GetValue(wsClient);
            if (_websocketClient is null)
            {
                throw new InvalidOperationException(
                    "TwitchLib's _websocketClient field has not been initialized."
                );
            }

            var websocketType = _websocketClient.GetType();
            _onDataReceivedEvent = websocketType.GetEvent(
                "OnDataReceived",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            if (_onDataReceivedEvent is null)
            {
                throw new MissingMemberException(websocketType.FullName, "OnDataReceived");
            }

            var handlerType =
                _onDataReceivedEvent.EventHandlerType
                ?? throw new InvalidOperationException(
                    "TwitchLib's OnDataReceived event has no handler type."
                );
            var invokeMethod =
                handlerType.GetMethod("Invoke")
                ?? throw new InvalidOperationException(
                    "TwitchLib's OnDataReceived delegate has no Invoke method."
                );
            var parameters = invokeMethod.GetParameters();
            if (parameters.Length != 2)
            {
                throw new InvalidOperationException(
                    "TwitchLib's OnDataReceived delegate has an unexpected signature."
                );
            }

            _bytesProperty = parameters[1]
                .ParameterType.GetProperty("Bytes", BindingFlags.Instance | BindingFlags.Public);
            if (_bytesProperty is null)
            {
                throw new MissingMemberException(parameters[1].ParameterType.FullName, "Bytes");
            }

            var handlerMethod =
                GetType()
                    .GetMethod(
                        nameof(OnDataReceivedHandler),
                        BindingFlags.Instance | BindingFlags.NonPublic
                    )
                ?? throw new MissingMethodException(
                    GetType().FullName,
                    nameof(OnDataReceivedHandler)
                );

            // The TwitchLib delegate returns Task. Binding to its actual delegate
            // type is essential: EventHandler (void) is not compatible and silently
            // prevented the original implementation from subscribing.
            _handler = Delegate.CreateDelegate(
                handlerType,
                this,
                handlerMethod,
                throwOnBindFailure: true
            );
            _onDataReceivedEvent.AddEventHandler(_websocketClient, _handler);
            _subscribed = true;

            logger.LogInformation(
                "EventSubRawForwarder subscribed to TwitchLib {WebsocketClientType}.{EventName}",
                websocketType.FullName,
                _onDataReceivedEvent.Name
            );
        }
        catch (Exception ex)
        {
            MarsMetrics.EventSubRawErrors.Add(1);
            logger.LogError(ex, "EventSubRawForwarder could not subscribe to raw Twitch frames");
        }
    }

    private void TryUnsubscribe()
    {
        if (
            !_subscribed
            || _websocketClient is null
            || _onDataReceivedEvent is null
            || _handler is null
        )
        {
            return;
        }

        try
        {
            _onDataReceivedEvent.RemoveEventHandler(_websocketClient, _handler);
        }
        catch (Exception ex)
        {
            MarsMetrics.EventSubRawErrors.Add(1);
            logger.LogWarning(ex, "EventSubRawForwarder could not remove the raw frame handler");
        }
        finally
        {
            _subscribed = false;
        }
    }

    // AsyncEventHandler<T> requires Task(object?, T). The object parameter lets
    // this assembly handle TwitchLib's internal DataReceivedArgs without taking a
    // compile-time dependency on an inaccessible type. This method only copies
    // bytes and writes to a bounded channel; it never parses JSON or waits for I/O.
    private Task OnDataReceivedHandler(object? sender, object? args)
    {
        try
        {
            if (
                args is null
                || _bytesProperty?.GetValue(args) is not byte[] bytes
                || bytes.Length == 0
            )
            {
                return Task.CompletedTask;
            }

            MarsMetrics.EventSubRawForwarded.Add(1);

            var frame = new PendingFrame(
                (byte[])bytes.Clone(),
                wsClient.SessionId,
                DateTimeOffset.UtcNow,
                Activity.Current?.Id
            );

            if (!_channel.Writer.TryWrite(frame))
            {
                MarsMetrics.EventSubRawDropped.Add(1);
            }
        }
        catch (Exception ex)
        {
            MarsMetrics.EventSubRawErrors.Add(1);
            logger.LogDebug(ex, "EventSubRawForwarder failed to enqueue a raw Twitch frame");
        }

        return Task.CompletedTask;
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var frame in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await PublishRawFrameAsync(frame, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    MarsMetrics.EventSubRawErrors.Add(1);
                    logger.LogWarning(
                        ex,
                        "EventSubRawForwarder could not publish a raw Twitch frame"
                    );
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The host is stopping or the bounded shutdown timeout was reached.
        }
        catch (Exception ex)
        {
            MarsMetrics.EventSubRawErrors.Add(1);
            logger.LogError(ex, "EventSubRawForwarder background worker stopped unexpectedly");
        }
    }

    private async Task PublishRawFrameAsync(PendingFrame frame, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(frame.Bytes);
        var root = document.RootElement;
        var metadata = GetObjectProperty(root, "metadata");
        var payload = GetObjectProperty(root, "payload");
        var subscription = GetObjectProperty(payload, "subscription");

        var messageType = GetStringProperty(metadata, "message_type") ?? "unknown";
        var subscriptionType = GetStringProperty(metadata, "subscription_type");

        var envelope = new Dictionary<string, object?>
        {
            ["schemaVersion"] = 1,
            ["messageId"] = GetStringProperty(metadata, "message_id"),
            ["messageType"] = messageType,
            ["subscriptionType"] = subscriptionType,
            ["subscriptionVersion"] = GetStringProperty(metadata, "subscription_version"),
            ["subscriptionId"] = GetStringProperty(subscription, "id"),
            ["sessionId"] = frame.SessionId,
            ["eventName"] = subscriptionType ?? messageType,
            ["observedAtUtc"] = frame.ObservedAtUtc,
            ["sourceService"] = "MARS.TwitchCore",
            ["traceParent"] = frame.TraceParent,
            // Clone detaches the JsonElement from the disposed JsonDocument and
            // preserves the complete original frame, including unknown fields.
            ["raw"] = root.Clone(),
        };

        await eventBus.PublishAsync(RoutingKey, envelope, cancellationToken).ConfigureAwait(false);
        MarsMetrics.EventSubRawPublished.Add(1);
    }

    private static JsonElement GetObjectProperty(JsonElement parent, string propertyName)
    {
        if (
            parent.ValueKind == JsonValueKind.Object
            && parent.TryGetProperty(propertyName, out var value)
            && value.ValueKind == JsonValueKind.Object
        )
        {
            return value;
        }

        return default;
    }

    private static string? GetStringProperty(JsonElement parent, string propertyName)
    {
        if (
            parent.ValueKind == JsonValueKind.Object
            && parent.TryGetProperty(propertyName, out var value)
            && value.ValueKind == JsonValueKind.String
        )
        {
            return value.GetString();
        }

        return null;
    }

    private static async Task ObserveWorkerAsync(Task workerTask)
    {
        try
        {
            await workerTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected after the shutdown timeout.
        }
    }

    private sealed record PendingFrame(
        byte[] Bytes,
        string? SessionId,
        DateTimeOffset ObservedAtUtc,
        string? TraceParent
    );
}
