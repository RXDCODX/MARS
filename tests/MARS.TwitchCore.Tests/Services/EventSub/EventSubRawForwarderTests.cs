using System;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MARS.Shared.Messaging;
using MARS.TwitchCore.Services.EventSub;
using Microsoft.Extensions.Logging.Abstractions;
using TwitchLib.EventSub.Websockets;

namespace MARS.TwitchCore.Tests.Services.EventSub;

public class EventSubRawForwarderTests
{
    [Fact]
    public async Task ForwardsOriginalFrameFromTwitchLibAsyncEvent()
    {
        var wsClient = new EventSubWebsocketClient();
        var eventBus = new RecordingEventBus();
        var forwarder = new EventSubRawForwarder(
            wsClient,
            eventBus,
            NullLogger<EventSubRawForwarder>.Instance
        );

        object? underlyingWebsocketClient = null;

        const string rawJson =
            """{"metadata":{"message_id":"message-1","message_type":"session_keepalive","message_timestamp":"2026-11-02T06:00:00.000Z"},"payload":{"future_property":{"must_survive":true}}}""";

        try
        {
            await forwarder.StartAsync(TestContext.Current.CancellationToken);

            var websocketClientField = typeof(EventSubWebsocketClient).GetField(
                "_websocketClient",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.NotNull(websocketClientField);

            underlyingWebsocketClient = websocketClientField.GetValue(wsClient);
            Assert.NotNull(underlyingWebsocketClient);

            var rawEvent = underlyingWebsocketClient
                .GetType()
                .GetEvent("OnDataReceived", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(rawEvent);
            Assert.NotNull(rawEvent.EventHandlerType);

            var eventArgsType = rawEvent.EventHandlerType.GetGenericArguments()[0];
            var eventArgs = Activator.CreateInstance(eventArgsType, nonPublic: true);
            Assert.NotNull(eventArgs);

            var bytesProperty = eventArgsType.GetProperty(
                "Bytes",
                BindingFlags.Instance | BindingFlags.Public
            );
            Assert.NotNull(bytesProperty);
            bytesProperty.SetValue(eventArgs, Encoding.UTF8.GetBytes(rawJson));

            // Invoke TwitchLib's real multicast event. Its own parser is called
            // first; the forwarding handler must coexist with it and return Task.
            var eventField = Array.Find(
                underlyingWebsocketClient
                    .GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.NonPublic),
                field => field.FieldType == rawEvent.EventHandlerType
            );
            Assert.NotNull(eventField);

            var handlers = Assert.IsAssignableFrom<Delegate>(
                eventField.GetValue(underlyingWebsocketClient)
            );
            var dispatchTask = Assert.IsAssignableFrom<Task>(
                handlers.DynamicInvoke(underlyingWebsocketClient, eventArgs)
            );
            await dispatchTask.WaitAsync(
                TimeSpan.FromSeconds(3),
                TestContext.Current.CancellationToken
            );
            await eventBus.Published.Task.WaitAsync(
                TimeSpan.FromSeconds(3),
                TestContext.Current.CancellationToken
            );

            Assert.Equal("twitch.eventsub.raw", eventBus.RoutingKey);
            Assert.NotNull(eventBus.Message);

            using var envelope = JsonDocument.Parse(eventBus.Message);
            var root = envelope.RootElement;

            Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
            Assert.Equal("message-1", root.GetProperty("messageId").GetString());
            Assert.Equal("session_keepalive", root.GetProperty("messageType").GetString());
            Assert.Equal("session_keepalive", root.GetProperty("eventName").GetString());
            Assert.Equal("MARS.TwitchCore", root.GetProperty("sourceService").GetString());
            Assert.True(
                root.GetProperty("observedAtUtc").GetDateTimeOffset() > DateTimeOffset.MinValue
            );

            var forwardedRaw = root.GetProperty("raw");
            Assert.Equal(rawJson, forwardedRaw.GetRawText());
            Assert.True(
                forwardedRaw
                    .GetProperty("payload")
                    .GetProperty("future_property")
                    .GetProperty("must_survive")
                    .GetBoolean()
            );
        }
        finally
        {
            await forwarder.StopAsync(CancellationToken.None);
            await forwarder.DisposeAsync();

            if (underlyingWebsocketClient is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private sealed class RecordingEventBus : IMarsEventBus
    {
        public string? RoutingKey { get; private set; }
        public string? Message { get; private set; }
        public TaskCompletionSource<bool> Published { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task PublishAsync<T>(
            string routingKey,
            T message,
            CancellationToken cancellationToken = default
        )
        {
            RoutingKey = routingKey;
            Message = JsonSerializer.Serialize(message);
            Published.TrySetResult(true);

            return Task.CompletedTask;
        }
    }
}
