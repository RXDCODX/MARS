using System.Text.Json;
using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Shared.Tests.Messaging;

public sealed class TwitchEventSubConsumerTests
{
    private const string QueueName = "mars.test.twitch-eventsub";

    [Fact]
    public async Task NotificationIsDeserializedFromSnakeCaseAndIncludesRawContext()
    {
        var consumer = new TestConsumer(TwitchEventSubTypes.ChannelPointsCustomRewardRedemptionAdd);

        await consumer.ProcessAsync(RabbitMqConfig.TwitchEventSubRaw, RedemptionEnvelope);

        var twitchEvent = Assert.IsType<RedemptionEvent>(consumer.ReceivedEvent);
        Assert.Equal("redemption-1", twitchEvent.Id);
        Assert.Equal("broadcaster-1", twitchEvent.BroadcasterUserId);
        Assert.Equal("user-1", twitchEvent.UserId);
        Assert.Equal("viewer", twitchEvent.UserName);
        Assert.Equal("asp", twitchEvent.UserInput);
        Assert.Equal("reward-160", twitchEvent.Reward?.Id);
        Assert.Equal(160, twitchEvent.Reward?.Cost);

        var context = Assert.IsType<TwitchEventSubContext>(consumer.ReceivedContext);
        Assert.Equal(1, context.SchemaVersion);
        Assert.Equal("message-1", context.MessageId);
        Assert.Equal(
            TwitchEventSubTypes.ChannelPointsCustomRewardRedemptionAdd,
            context.SubscriptionType
        );
        Assert.Equal("2", context.SubscriptionVersion);
        Assert.Equal("subscription-1", context.SubscriptionId);
        Assert.Equal("MARS.TwitchCore", context.SourceService);
        Assert.True(
            context
                .RawMessage.GetProperty("payload")
                .GetProperty("event")
                .GetProperty("future_property")
                .GetProperty("preserve")
                .GetBoolean()
        );
    }

    [Fact]
    public async Task NotificationWithDifferentSubscriptionTypeIsIgnored()
    {
        var consumer = new TestConsumer(TwitchEventSubTypes.ChannelPointsCustomRewardRedemptionAdd);
        var envelope = RedemptionEnvelope.Replace(
            TwitchEventSubTypes.ChannelPointsCustomRewardRedemptionAdd,
            TwitchEventSubTypes.StreamOnline,
            StringComparison.Ordinal
        );

        await consumer.ProcessAsync(RabbitMqConfig.TwitchEventSubRaw, envelope);

        Assert.Null(consumer.ReceivedEvent);
        Assert.Null(consumer.ReceivedContext);
    }

    [Fact]
    public async Task WebsocketControlFramesAreIgnoredByTypedConsumer()
    {
        var consumer = new TestConsumer(TwitchEventSubTypes.ChannelPointsCustomRewardRedemptionAdd);

        await consumer.ProcessAsync(
            RabbitMqConfig.TwitchEventSubRaw,
            """
            {
              "schemaVersion": 1,
              "messageType": "session_keepalive",
              "raw": { "metadata": { "message_type": "session_keepalive" }, "payload": {} }
            }
            """
        );

        Assert.Null(consumer.ReceivedEvent);
    }

    [Fact]
    public async Task NotificationWithoutEventPayloadIsRejected()
    {
        var consumer = new TestConsumer(TwitchEventSubTypes.ChannelPointsCustomRewardRedemptionAdd);

        await Assert.ThrowsAsync<JsonException>(() =>
            consumer.ProcessAsync(
                RabbitMqConfig.TwitchEventSubRaw,
                """
                {
                  "schemaVersion": 1,
                  "messageType": "notification",
                  "subscriptionType": "channel.channel_points_custom_reward_redemption.add",
                  "raw": { "payload": {} }
                }
                """
            )
        );
    }

    [Fact]
    public async Task EmptyFiltersAcceptAnyNotificationType()
    {
        var consumer = new TestConsumer();

        await consumer.ProcessAsync(RabbitMqConfig.TwitchEventSubRaw, RedemptionEnvelope);

        Assert.NotNull(consumer.ReceivedEvent);
    }

    private const string RedemptionEnvelope = """
        {
          "schemaVersion": 1,
          "messageId": "message-1",
          "messageType": "notification",
          "subscriptionType": "channel.channel_points_custom_reward_redemption.add",
          "subscriptionVersion": "2",
          "subscriptionId": "subscription-1",
          "sessionId": "session-1",
          "eventName": "channel.channel_points_custom_reward_redemption.add",
          "observedAtUtc": "2026-11-02T12:00:00Z",
          "sourceService": "MARS.TwitchCore",
          "traceParent": "00-trace",
          "raw": {
            "metadata": {
              "message_id": "message-1",
              "message_type": "notification",
              "subscription_type": "channel.channel_points_custom_reward_redemption.add",
              "subscription_version": "2"
            },
            "payload": {
              "subscription": { "id": "subscription-1", "type": "channel.channel_points_custom_reward_redemption.add" },
              "event": {
                "id": "redemption-1",
                "broadcaster_user_id": "broadcaster-1",
                "user_id": "user-1",
                "user_name": "viewer",
                "user_input": "asp",
                "redeemed_at": "2026-11-02T11:59:00Z",
                "reward": {
                  "id": "reward-160",
                  "title": "Leg Bum",
                  "cost": 160
                },
                "future_property": { "preserve": true }
              }
            }
          }
        }
        """;

    private static RabbitMqOptions CreateOptions() =>
        new()
        {
            Host = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
        };

    private sealed class TestConsumer : TwitchEventSubConsumer<RedemptionEvent>
    {
        public TestConsumer(params string[] subscriptionTypes)
            : base(
                CreateOptions(),
                "shared-tests",
                QueueName,
                NullLogger<TwitchEventSubConsumerTests>.Instance,
                subscriptionTypes
            ) { }

        public RedemptionEvent? ReceivedEvent { get; private set; }
        public TwitchEventSubContext? ReceivedContext { get; private set; }

        public Task ProcessAsync(string routingKey, string json) =>
            HandleMessageAsync(routingKey, json, TestContext.Current.CancellationToken);

        protected override Task HandleEventAsync(
            RedemptionEvent twitchEvent,
            TwitchEventSubContext context,
            CancellationToken cancellationToken
        )
        {
            ReceivedEvent = twitchEvent;
            ReceivedContext = context;
            return Task.CompletedTask;
        }
    }

    public sealed class RedemptionEvent
    {
        public RedemptionEvent() { }

        public string? Id { get; init; }
        public string? BroadcasterUserId { get; init; }
        public string? UserId { get; init; }
        public string? UserName { get; init; }
        public string? UserInput { get; init; }
        public DateTimeOffset RedeemedAt { get; init; }
        public RewardEvent? Reward { get; init; }
    }

    public sealed class RewardEvent
    {
        public RewardEvent() { }

        public string? Id { get; init; }
        public string? Title { get; init; }
        public int Cost { get; init; }
    }
}
