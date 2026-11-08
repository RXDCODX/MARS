using System.Text.Json;
using MARS.Alerts.Application;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Alerts.Tests.Services;

/// <summary>Проверяет typed Wolverine listeners без запуска RabbitMQ.</summary>
public class TwitchMessagingListenersTests
{
    [Fact]
    public async Task TwitchMessagesListener_ReceivesChatMessage()
    {
        var listener = new ChatProbe();
        var message = new ChatEventMessageEvent
        {
            UserId = "42",
            UserName = "viewer",
            Message = "hello",
        };

        await listener.Handle(message, TestContext.Current.CancellationToken);

        Assert.Equal("hello", Assert.Single(listener.Messages).Message);
    }

    [Fact]
    public async Task TwitchRewardInputMessageListener_ReceivesSeparateContract()
    {
        var listener = new RewardInputProbe();
        var message = new RewardInputChatMessageEvent
        {
            Message = new ChatEventMessageEvent
            {
                UserId = "42",
                UserName = "viewer",
                Message = "reward text",
                CustomRewardId = "reward-1",
            },
        };

        await listener.Handle(message, TestContext.Current.CancellationToken);

        var received = Assert.Single(listener.Messages);
        Assert.Equal("reward text", received.Message);
        Assert.Equal("reward-1", received.CustomRewardId);
    }

    [Fact]
    public async Task TwitchMessageDeletedListener_ReceivesDeletedMessage()
    {
        var listener = new DeletedMessageProbe();
        var message = new ChatMessageDeletedEvent { MessageId = "message-1", Channel = "mars" };

        await listener.Handle(message, TestContext.Current.CancellationToken);

        var received = Assert.Single(listener.Messages);
        Assert.Equal("message-1", received.MessageId);
        Assert.Equal("mars", received.Channel);
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

        await listener.Handle(eventSubEvent, TestContext.Current.CancellationToken);

        var received = Assert.Single(listener.Events);
        Assert.Equal("event-1", received.MessageId);
        Assert.Equal("channel.follow", received.EventName);
        Assert.Equal(
            "channel.follow",
            received.Raw.GetProperty("subscription").GetProperty("type").GetString()
        );
    }

    private sealed class ChatProbe : TwitchMessagesListener
    {
        public List<ChatEventMessageEvent> Messages { get; } = [];

        public ChatProbe()
            : base(NullLogger<TwitchMessagesListener>.Instance) { }

        protected override Task OnMessageReceivedAsync(
            ChatEventMessageEvent message,
            CancellationToken ct
        )
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class RewardInputProbe : TwitchRewardInputMessageListener
    {
        public List<ChatEventMessageEvent> Messages { get; } = [];

        public RewardInputProbe()
            : base(NullLogger<TwitchRewardInputMessageListener>.Instance) { }

        protected override Task OnRewardInputReceivedAsync(
            ChatEventMessageEvent message,
            CancellationToken ct
        )
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class DeletedMessageProbe : TwitchMessageDeletedListener
    {
        public List<ChatMessageDeletedEvent> Messages { get; } = [];

        public DeletedMessageProbe()
            : base(NullLogger<TwitchMessageDeletedListener>.Instance) { }

        protected override Task OnMessageDeletedAsync(
            ChatMessageDeletedEvent message,
            CancellationToken ct
        )
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class EventSubProbe : TwitchEventSubListener
    {
        public List<EventSubRawEvent> Events { get; } = [];

        public EventSubProbe()
            : base(NullLogger<TwitchEventSubListener>.Instance) { }

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
