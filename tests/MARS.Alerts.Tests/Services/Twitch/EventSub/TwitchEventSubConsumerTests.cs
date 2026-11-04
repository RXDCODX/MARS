using System.Reflection;
using System.Text.Json;
using MARS.Alerts.Services.Twitch.EventSub;
using MARS.Alerts.Services.Twitch.Rewards.Activation;
using MARS.Shared.Configuration;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Alerts.Tests.Services.Twitch.EventSub;

public class TwitchEventSubConsumerTests
{
    private readonly Mock<ITelegramusNotifier> _notifier = new();
    private readonly Mock<IRewardRedemptionProcessor> _rewardProcessor = new();
    private readonly List<string> _forwarded = [];

    public TwitchEventSubConsumerTests()
    {
        _notifier
            .Setup(notifier => notifier.TwitchEventSub(It.IsAny<string>()))
            .Callback<string>(json => _forwarded.Add(json))
            .Returns(Task.CompletedTask);

        _rewardProcessor
            .Setup(processor =>
                processor.ProcessAsync(
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((string raw, string? _, Guid? _, CancellationToken _) => raw);
    }

    [Fact]
    public async Task NotificationIsForwardedAsRawJson()
    {
        const string raw = """
            {"metadata":{"message_type":"notification","subscription_type":"channel.follow"},"payload":{"event":{"user_name":"Pyro"}}}
            """;

        await HandleAsync(CreateConsumer(), Envelope("notification", raw));

        Assert.Equal(raw, Assert.Single(_forwarded));
    }

    [Fact]
    public async Task DuplicateEventSubMessageIsForwardedOnlyOnce()
    {
        const string raw = """
            {"metadata":{"message_id":"event-message-duplicate","message_type":"notification","subscription_type":"channel.follow"},"payload":{"event":{"user_name":"Pyro"}}}
            """;
        var consumer = CreateConsumer();
        var envelope = Envelope("notification", raw);

        await HandleAsync(consumer, envelope);
        await HandleAsync(consumer, envelope);

        Assert.Single(_forwarded);
    }

    [Fact]
    public async Task UnknownSubscriptionTypeIsForwardedWithoutRegistration()
    {
        const string raw = """
            {"metadata":{"message_type":"notification","subscription_type":"channel.new_future_event"},"payload":{"event":{"custom_field":42}}}
            """;

        await HandleAsync(CreateConsumer(), Envelope("notification", raw));

        using var received = JsonDocument.Parse(Assert.Single(_forwarded));
        Assert.Equal(
            "channel.new_future_event",
            received
                .RootElement.GetProperty("metadata")
                .GetProperty("subscription_type")
                .GetString()
        );
        Assert.Equal(
            42,
            received
                .RootElement.GetProperty("payload")
                .GetProperty("event")
                .GetProperty("custom_field")
                .GetInt32()
        );
    }

    [Fact]
    public async Task RedemptionUsesProcessorAndForwardsEnrichedRawEventOnSameHubMethod()
    {
        const string raw = """
            {"metadata":{"message_id":"eventsub-1","message_type":"notification","subscription_type":"channel.channel_points_custom_reward_redemption.add"},"payload":{"event":{"id":"redemption-1","reward":{"id":"reward-1"}}}}
            """;
        const string enriched = """
            {"metadata":{"message_id":"eventsub-1"},"payload":{"event":{"id":"redemption-1"}},"mars":{"reward":{"type":"mars.reward.activated"}}}
            """;
        var mediaInfoId = Guid.NewGuid();

        _rewardProcessor
            .Setup(processor =>
                processor.ProcessAsync(
                    raw,
                    "special-miku",
                    mediaInfoId,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(enriched);

        await HandleAsync(
            CreateConsumer(),
            Envelope("notification", raw, new { alertKey = "special-miku", mediaInfoId })
        );

        _rewardProcessor.Verify(
            processor =>
                processor.ProcessAsync(
                    raw,
                    "special-miku",
                    mediaInfoId,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        Assert.Equal(enriched, Assert.Single(_forwarded));
        _notifier.Verify(notifier => notifier.TwitchEventSub(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RedemptionWithoutLocalRewardMetadataStillUsesGenericProcessor()
    {
        const string raw = """
            {"metadata":{"message_id":"eventsub-2","message_type":"notification","subscription_type":"channel.channel_points_custom_reward_redemption.add"},"payload":{"event":{"id":"redemption-2","reward":{"id":"unknown-reward"}}}}
            """;

        await HandleAsync(CreateConsumer(), Envelope("notification", raw));

        _rewardProcessor.Verify(
            processor => processor.ProcessAsync(raw, null, null, It.IsAny<CancellationToken>()),
            Times.Once
        );
        Assert.Equal(raw, Assert.Single(_forwarded));
    }

    [Fact]
    public async Task SuppressedRedemptionDoesNotSendHubMessageButIsDeduplicated()
    {
        const string raw = """
            {"metadata":{"message_id":"eventsub-suppressed","message_type":"notification","subscription_type":"channel.channel_points_custom_reward_redemption.add"},"payload":{"event":{"id":"redemption-3","reward":{"id":"reward-3"}}}}
            """;

        _rewardProcessor
            .Setup(processor =>
                processor.ProcessAsync(
                    raw,
                    It.IsAny<string?>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((string?)null);

        var consumer = CreateConsumer();
        var envelope = Envelope("notification", raw);

        await HandleAsync(consumer, envelope);
        await HandleAsync(consumer, envelope);

        Assert.Empty(_forwarded);
        _rewardProcessor.Verify(
            processor =>
                processor.ProcessAsync(
                    raw,
                    It.IsAny<string?>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Theory]
    [InlineData("session_welcome")]
    [InlineData("session_keepalive")]
    [InlineData("session_reconnect")]
    [InlineData("revocation")]
    public async Task NonNotificationFrameIsNotForwarded(string messageType)
    {
        const string raw = """{"metadata":{"subscription_type":"channel.follow"},"payload":{}}""";

        await HandleAsync(CreateConsumer(), Envelope(messageType, raw));

        Assert.Empty(_forwarded);
        _rewardProcessor.VerifyNoOtherCalls();
    }

    private TwitchEventSubConsumer CreateConsumer() =>
        new(
            Options.Create(new RabbitMqOptions()),
            _notifier.Object,
            _rewardProcessor.Object,
            NullLogger<TwitchEventSubConsumer>.Instance
        );

    private static async Task HandleAsync(TwitchEventSubConsumer consumer, string json)
    {
        var method = typeof(TwitchEventSubConsumer).GetMethod(
            "HandleMessageAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        await (Task)
            method.Invoke(
                consumer,
                [
                    RabbitMqConfig.RoutingKeys.EventSubReceived,
                    json,
                    TestContext.Current.CancellationToken,
                ]
            )!;
    }

    private static string Envelope(string messageType, string raw, object? rewardMetadata = null)
    {
        using var document = JsonDocument.Parse(raw);

        return JsonSerializer.Serialize(
            new
            {
                schemaVersion = 1,
                messageId = "event-message-1",
                messageType,
                rewardMetadata,
                raw = document.RootElement,
            }
        );
    }
}
