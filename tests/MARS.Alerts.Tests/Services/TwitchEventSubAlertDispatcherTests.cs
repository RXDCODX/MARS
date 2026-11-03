using System.Text.Json;
using MARS.Alerts.Services.Twitch;
using MARS.Shared.Configuration;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Alerts.Tests.Services;

public sealed class TwitchEventSubAlertDispatcherTests
{
    [Fact]
    public async Task Follow_notification_is_published_to_overlay()
    {
        var notifier = new Mock<ITelegramusNotifier>();
        var dispatcher = CreateDispatcher(notifier.Object);

        await dispatcher.DispatchAsync(
            Notification(
                TwitchEventSubTypes.ChannelFollow,
                new TwitchChannelFollowEvent
                {
                    UserId = "user-1",
                    UserLogin = "viewer",
                    UserName = "Viewer",
                    FollowedAt = DateTimeOffset.Parse("2026-11-02T13:00:00Z"),
                }
            )
        );

        notifier.Verify(
            n =>
                n.TwitchEventAlert(
                    "message-1",
                    TwitchEventSubTypes.ChannelFollow,
                    "Новый подписчик",
                    "Viewer подписался на канал",
                    "user-1",
                    "viewer",
                    "Viewer",
                    0,
                    "2026-11-02T13:00:00.0000000+00:00"
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Raid_notification_includes_viewer_count()
    {
        var notifier = new Mock<ITelegramusNotifier>();
        var dispatcher = CreateDispatcher(notifier.Object);

        await dispatcher.DispatchAsync(
            Notification(
                TwitchEventSubTypes.ChannelRaid,
                new TwitchChannelRaidEvent
                {
                    FromBroadcasterUserId = "from-1",
                    FromBroadcasterUserLogin = "raider",
                    FromBroadcasterUserName = "Raider",
                    ToBroadcasterUserId = "to-1",
                    ToBroadcasterUserLogin = "channel",
                    ToBroadcasterUserName = "Channel",
                    Viewers = 42,
                }
            )
        );

        notifier.Verify(
            n =>
                n.TwitchEventAlert(
                    "message-1",
                    TwitchEventSubTypes.ChannelRaid,
                    "РЕЙД",
                    "Raider привёл 42 зрителей",
                    "from-1",
                    "raider",
                    "Raider",
                    42,
                    "2026-11-02T12:59:00.0000000+00:00"
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Redemption_is_left_to_existing_reward_pipeline()
    {
        var notifier = new Mock<ITelegramusNotifier>();
        var dispatcher = CreateDispatcher(notifier.Object);

        await dispatcher.DispatchAsync(
            Notification(
                TwitchEventSubTypes.ChannelPointsCustomRewardRedemptionAdd,
                new TwitchChannelPointsCustomRewardRedemptionAddedEvent
                {
                    Id = "redemption-1",
                    UserName = "Viewer",
                    Reward = new TwitchChannelPointsReward { Id = "reward-1", Cost = 160 },
                }
            )
        );

        notifier.Verify(
            n =>
                n.TwitchEventAlert(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<string>()
                ),
            Times.Never
        );
    }

    private static TwitchEventSubAlertDispatcher CreateDispatcher(ITelegramusNotifier notifier)
    {
        var consumer = new TwitchEventSubConsumer(
            Options.Create(new RabbitMqOptions()),
            "alerts-tests",
            "alerts.tests.twitch.eventsub",
            NullLogger<TwitchEventSubConsumer>.Instance
        );

        return new TwitchEventSubAlertDispatcher(
            consumer,
            notifier,
            NullLogger<TwitchEventSubAlertDispatcher>.Instance
        );
    }

    private static TwitchEventSubNotification Notification(string type, object eventData)
    {
        using var document = JsonDocument.Parse("""{"metadata":{},"payload":{"event":{}}}""");
        return new TwitchEventSubNotification(
            SchemaVersion: 1,
            MessageId: "message-1",
            MessageType: "notification",
            SubscriptionType: type,
            SubscriptionVersion: "1",
            SubscriptionId: "subscription-1",
            SessionId: "session-1",
            EventName: type,
            ObservedAtUtc: DateTimeOffset.Parse("2026-11-02T12:59:00Z"),
            SourceService: "MARS.TwitchCore",
            TraceParent: null,
            EventData: eventData,
            IsKnownEventType: true,
            RawMessage: document.RootElement.Clone(),
            RawEvent: document.RootElement.GetProperty("payload").GetProperty("event").Clone()
        );
    }
}
