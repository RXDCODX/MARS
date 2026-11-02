using System.Text.Json;
using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Tests.Messaging;

public sealed class TwitchEventSubConsumerTests
{
    private const string QueueName = "mars.test.twitch-eventsub";

    [Fact]
    public async Task SingleConsumerDetectsRedemptionAndRaisesTypedNotification()
    {
        var consumer = CreateConsumer();
        TwitchEventSubNotification? received = null;
        consumer.EventReceived += (_, notification) =>
        {
            received = notification;
            return Task.CompletedTask;
        };

        await consumer.ProcessMessageAsync(RabbitMqConfig.TwitchEventSubRaw, RedemptionEnvelope);

        Assert.NotNull(received);
        var redemption = Assert.IsType<TwitchChannelPointsCustomRewardRedemptionAddedEvent>(
            received.EventData
        );
        Assert.True(received.IsKnownEventType);
        Assert.Equal(
            typeof(TwitchChannelPointsCustomRewardRedemptionAddedEvent),
            received.EventClrType
        );
        Assert.Equal("redemption-1", redemption.Id);
        Assert.Equal("broadcaster-1", redemption.BroadcasterUserId);
        Assert.Equal("user-1", redemption.UserId);
        Assert.Equal("viewer", redemption.UserName);
        Assert.Equal("asp", redemption.UserInput);
        Assert.Equal("reward-160", redemption.Reward?.Id);
        Assert.Equal(160, redemption.Reward?.Cost);
        Assert.Equal("message-1", received.MessageId);
        Assert.Equal("subscription-1", received.SubscriptionId);
        Assert.Equal("MARS.TwitchCore", received.SourceService);
        Assert.True(
            received.RawEvent.GetProperty("future_property").GetProperty("preserve").GetBoolean()
        );
        Assert.True(
            received
                .RawMessage.GetProperty("payload")
                .GetProperty("event")
                .GetProperty("future_property")
                .GetProperty("preserve")
                .GetBoolean()
        );
    }

    [Fact]
    public async Task SameConsumerSelectsDifferentModelForRaid()
    {
        var consumer = CreateConsumer();
        TwitchEventSubNotification? received = null;
        consumer.EventReceived += (_, notification) =>
        {
            received = notification;
            return Task.CompletedTask;
        };

        await consumer.ProcessMessageAsync(RabbitMqConfig.TwitchEventSubRaw, RaidEnvelope);

        Assert.NotNull(received);
        var raid = Assert.IsType<TwitchChannelRaidEvent>(received.EventData);
        Assert.Equal("source-channel", raid.FromBroadcasterUserLogin);
        Assert.Equal("target-channel", raid.ToBroadcasterUserLogin);
        Assert.Equal(42, raid.Viewers);
    }

    [Fact]
    public async Task UnknownSubscriptionTypeIsDeliveredAsRawJsonElement()
    {
        var consumer = CreateConsumer();
        TwitchEventSubNotification? received = null;
        consumer.EventReceived += (_, notification) =>
        {
            received = notification;
            return Task.CompletedTask;
        };

        await consumer.ProcessMessageAsync(RabbitMqConfig.TwitchEventSubRaw, UnknownEnvelope);

        Assert.NotNull(received);
        Assert.False(received.IsKnownEventType);
        var rawEvent = Assert.IsType<JsonElement>(received.EventData);
        Assert.Equal("future-value", rawEvent.GetProperty("new_field").GetString());
    }

    [Fact]
    public async Task ApplicationCanRegisterAdditionalPayloadModel()
    {
        var consumer = CreateConsumer();
        consumer.RegisterEventType<CustomEvent>("custom.event");
        TwitchEventSubNotification? received = null;
        consumer.EventReceived += (_, notification) =>
        {
            received = notification;
            return Task.CompletedTask;
        };

        await consumer.ProcessMessageAsync(RabbitMqConfig.TwitchEventSubRaw, CustomEnvelope);

        Assert.NotNull(received);
        var custom = Assert.IsType<CustomEvent>(received.EventData);
        Assert.Equal("registered-by-app", custom.Value);
        Assert.True(received.IsKnownEventType);
    }

    [Fact]
    public async Task AsyncApplicationHandlersAreAllAwaited()
    {
        var consumer = CreateConsumer();
        var calls = new List<int>();
        consumer.EventReceived += async (_, _) =>
        {
            await Task.Yield();
            calls.Add(1);
        };
        consumer.EventReceived += async (_, _) =>
        {
            await Task.Yield();
            calls.Add(2);
        };

        await consumer.ProcessMessageAsync(RabbitMqConfig.TwitchEventSubRaw, RedemptionEnvelope);

        Assert.Equal(new[] { 1, 2 }, calls);
    }

    [Fact]
    public async Task WebsocketControlFramesDoNotRaiseApplicationEvent()
    {
        var consumer = CreateConsumer();
        var raised = false;
        consumer.EventReceived += (_, _) =>
        {
            raised = true;
            return Task.CompletedTask;
        };

        await consumer.ProcessMessageAsync(
            RabbitMqConfig.TwitchEventSubRaw,
            """
            {
              "schemaVersion": 1,
              "messageType": "session_keepalive",
              "raw": { "metadata": { "message_type": "session_keepalive" }, "payload": {} }
            }
            """
        );

        Assert.False(raised);
    }

    [Fact]
    public async Task NotificationWithoutEventPayloadIsRejected()
    {
        var consumer = CreateConsumer();

        await Assert.ThrowsAsync<JsonException>(() =>
            consumer.ProcessMessageAsync(
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

    private static TwitchEventSubConsumer CreateConsumer() =>
        new(
            Options.Create(
                new RabbitMqOptions
                {
                    Host = "localhost",
                    Port = 5672,
                    UserName = "guest",
                    Password = "guest",
                }
            ),
            "shared-tests",
            QueueName,
            NullLogger<TwitchEventSubConsumer>.Instance
        );

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

    private const string RaidEnvelope = """
        {
          "schemaVersion": 1,
          "messageType": "notification",
          "subscriptionType": "channel.raid",
          "raw": {
            "payload": {
              "event": {
                "from_broadcaster_user_id": "from-id",
                "from_broadcaster_user_login": "source-channel",
                "from_broadcaster_user_name": "Source Channel",
                "to_broadcaster_user_id": "to-id",
                "to_broadcaster_user_login": "target-channel",
                "to_broadcaster_user_name": "Target Channel",
                "viewers": 42
              }
            }
          }
        }
        """;

    private const string UnknownEnvelope = """
        {
          "schemaVersion": 1,
          "messageType": "notification",
          "subscriptionType": "future.event",
          "raw": { "payload": { "event": { "new_field": "future-value" } } }
        }
        """;

    private const string CustomEnvelope = """
        {
          "schemaVersion": 1,
          "messageType": "notification",
          "subscriptionType": "custom.event",
          "raw": { "payload": { "event": { "value": "registered-by-app" } } }
        }
        """;

    public sealed record CustomEvent
    {
        public string? Value { get; init; }
    }
}
