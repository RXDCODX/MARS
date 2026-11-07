using System.Text.Json;
using MARS.Alerts.Services.Twitch.Messages;
using MARS.Platform.Web.AspNetCore.Infrastructure.Configuration;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Tests.Services;

public class TwitchMessagingListenersTests
{
    [Fact]
    public async Task TwitchMessagesListener_ReceivesRegularAndRewardInputMessages()
    {
        var listener = new MessagesProbe();
        var message = new ChatEventMessageEvent
        {
            UserId = "42",
            UserName = "viewer",
            Message = "hello",
        };
        var json = JsonSerializer.Serialize(message);
        var ct = TestContext.Current.CancellationToken;

        await listener.InvokeAsync(RabbitMqConfig.MessageReceived, json, ct);
        await listener.InvokeAsync(RabbitMqConfig.RewardInputMessage, json, ct);

        Assert.Equal(
            new[] { RabbitMqConfig.MessageReceived, RabbitMqConfig.RewardInputMessage },
            listener.ReceivedMessages.Select(item => item.RoutingKey)
        );
        Assert.All(listener.ReceivedMessages, item => Assert.Equal("hello", item.Message.Message));
    }

    [Fact]
    public async Task TwitchMessagesListener_ReceivesDeletedMessage()
    {
        var listener = new MessagesProbe();
        var message = new TwitchMessageDeletedEvent { MessageId = "message-1", Channel = "mars" };

        await listener.InvokeAsync(
            RabbitMqConfig.MessageDeleted,
            JsonSerializer.Serialize(message),
            TestContext.Current.CancellationToken
        );

        var received = Assert.Single(listener.DeletedMessages);
        Assert.Equal("message-1", received.MessageId);
        Assert.Equal("mars", received.Channel);
    }

    [Fact]
    public async Task TwitchMessagesListener_RejectsNullPayload()
    {
        var listener = new MessagesProbe();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            listener.InvokeAsync(
                RabbitMqConfig.MessageReceived,
                "null",
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task TwitchEventSubListener_ReceivesRawEventSubEnvelope()
    {
        var listener = new EventSubProbe();
        using var rawDocument = JsonDocument.Parse(
            """{"subscription":{"type":"channel.follow"}}"""
        );
        var eventSubEvent = new EventSubRawEvent
        {
            MessageId = "event-1",
            MessageType = "notification",
            SubscriptionType = "channel.follow",
            EventName = "channel.follow",
            Raw = rawDocument.RootElement.Clone(),
        };

        await listener.InvokeAsync(
            RabbitMqConfig.EventSubRaw,
            JsonSerializer.Serialize(eventSubEvent),
            TestContext.Current.CancellationToken
        );

        var received = Assert.Single(listener.Events);
        Assert.Equal("event-1", received.MessageId);
        Assert.Equal("channel.follow", received.EventName);
        Assert.Equal(
            "channel.follow",
            received.Raw.GetProperty("subscription").GetProperty("type").GetString()
        );
    }

    [Fact]
    public async Task TwitchEventSubListener_RejectsNullPayload()
    {
        var listener = new EventSubProbe();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            listener.InvokeAsync(
                RabbitMqConfig.EventSubRaw,
                "null",
                TestContext.Current.CancellationToken
            )
        );
    }

    private sealed class MessagesProbe : TwitchMessagesListener
    {
        public List<(string RoutingKey, ChatEventMessageEvent Message)> ReceivedMessages { get; } =
        [];

        public List<TwitchMessageDeletedEvent> DeletedMessages { get; } = [];

        public MessagesProbe()
            : base(
                Options.Create(new RabbitMqOptions()),
                NullLogger<TwitchMessagesListener>.Instance
            ) { }

        public Task InvokeAsync(string routingKey, string json, CancellationToken ct) =>
            HandleMessageAsync(routingKey, json, ct);

        protected override Task OnMessageReceivedAsync(
            string routingKey,
            ChatEventMessageEvent message,
            CancellationToken ct
        )
        {
            ReceivedMessages.Add((routingKey, message));
            return Task.CompletedTask;
        }

        protected override Task OnMessageDeletedAsync(
            TwitchMessageDeletedEvent message,
            CancellationToken ct
        )
        {
            DeletedMessages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class EventSubProbe : TwitchEventSubListener
    {
        public List<EventSubRawEvent> Events { get; } = [];

        public EventSubProbe()
            : base(
                Options.Create(new RabbitMqOptions()),
                NullLogger<TwitchEventSubListener>.Instance
            ) { }

        public Task InvokeAsync(string routingKey, string json, CancellationToken ct) =>
            HandleMessageAsync(routingKey, json, ct);

        protected override Task OnEventSubReceivedAsync(
            EventSubRawEvent eventSubEvent,
            CancellationToken ct
        )
        {
            Events.Add(eventSubEvent);
            return Task.CompletedTask;
        }
    }
}
