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
    private bool _reconnectHandlerSubscribed;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_workerTask is not null)
        {
            return Task.CompletedTask;
        }

        var cts = new CancellationTokenSource();
        _cts = cts;

        try
        {
            // TwitchLib replaces its private WebsocketClient instance on reconnect.
            // Observe that public lifecycle event so the raw handler follows the
            // replacement rather than remaining attached to the disposed socket.
            wsClient.WebsocketReconnected += OnWebsocketReconnected;
            _reconnectHandlerSubscribed = true;
            TrySubscribe();

            _workerTask = Task.Run(() => ProcessQueueAsync(cts.Token), cts.Token);
            return Task.CompletedTask;
        }
        catch
        {
            TryUnsubscribe();
            _channel.Writer.TryComplete();
            cts.Dispose();
            _cts = null;
            throw;
        }
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

            var currentWebsocketClient = field.GetValue(wsClient);
            if (currentWebsocketClient is null)
            {
                throw new InvalidOperationException(
                    "TwitchLib's _websocketClient field has not been initialized."
                );
            }

            // A reconnect replaces this object. Keep the handler attached to the
            // current instance, and avoid duplicate subscriptions on the same one.
            if (_subscribed && ReferenceEquals(_websocketClient, currentWebsocketClient))
            {
                return;
            }

            TryUnsubscribeRawHandler();
            _websocketClient = currentWebsocketClient;

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
            var typedHandlerMethod = handlerMethod.MakeGenericMethod(parameters[1].ParameterType);
            _handler = typedHandlerMethod.CreateDelegate(handlerType, this);

            // OnDataReceived is internal in TwitchLib 0.8.0. EventInfo.AddEventHandler
            // only invokes public accessors, so call the non-public add accessor
            // explicitly. This preserves the library's event invocation list.
            var addMethod =
                _onDataReceivedEvent.GetAddMethod(nonPublic: true)
                ?? throw new MissingMethodException(websocketType.FullName, "add_OnDataReceived");
            addMethod.Invoke(_websocketClient, [_handler]);
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
            logger.LogCritical(ex, "EventSubRawForwarder could not subscribe to raw Twitch frames");

            // Raw forwarding is a required part of this hosted service. Continuing
            // startup without the observer would look healthy while silently losing
            // every EventSub frame, so fail startup and expose the actual cause.
            throw new InvalidOperationException(
                "EventSubRawForwarder could not subscribe to TwitchLib raw frames.",
                ex
            );
        }
    }

    private Task OnWebsocketReconnected(object? sender, WebsocketReconnectedArgs args)
    {
        TrySubscribe();
        return Task.CompletedTask;
    }

    private void TryUnsubscribe()
    {
        if (_reconnectHandlerSubscribed)
        {
            wsClient.WebsocketReconnected -= OnWebsocketReconnected;
            _reconnectHandlerSubscribed = false;
        }

        TryUnsubscribeRawHandler();
    }

    private void TryUnsubscribeRawHandler()
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
            // As with add, TwitchLib's remove accessor is non-public.
            var removeMethod =
                _onDataReceivedEvent.GetRemoveMethod(nonPublic: true)
                ?? throw new MissingMethodException(
                    _websocketClient.GetType().FullName,
                    "remove_OnDataReceived"
                );
            removeMethod.Invoke(_websocketClient, [_handler]);
        }
        catch (Exception ex)
        {
            MarsMetrics.EventSubRawErrors.Add(1);
            logger.LogWarning(ex, "EventSubRawForwarder could not remove the raw frame handler");
        }
        finally
        {
            _subscribed = false;
            _onDataReceivedEvent = null;
            _bytesProperty = null;
            _websocketClient = null;
            _handler = null;
        }
    }

    // AsyncEventHandler<T> requires Task(object?, T). The object parameter lets
    // this assembly handle TwitchLib's internal DataReceivedArgs without taking a
    // compile-time dependency on an inaccessible type. This method only copies
    // bytes and writes to a bounded channel; it never parses JSON or waits for I/O.
    private Task OnDataReceivedHandler<TArgs>(object? sender, TArgs args)
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
