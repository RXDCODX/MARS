using System.Reflection;
using System.Text.Json;
using MARS.Alerts.Services.Twitch.EventSub;
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
    private readonly List<string> _forwarded = [];

    public TwitchEventSubConsumerTests()
    {
        _notifier
            .Setup(notifier => notifier.TwitchEventSub(It.IsAny<string>()))
            .Callback<string>(json => _forwarded.Add(json))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task NotificationIsForwardedAsRawJson()
    {
        const string raw = """
            {"metadata":{"message_type":"notification","subscription_type":"channel.follow"},"payload":{"event":{"user_name":"Pyro"}}}
            """;

        await HandleAsync(CreateConsumer(_notifier.Object), Envelope("notification", raw));

        Assert.Equal(raw, Assert.Single(_forwarded));
    }

    [Fact]
    public async Task DuplicateEventSubMessageIsForwardedOnlyOnce()
    {
        const string raw = """
            {"metadata":{"message_id":"event-message-duplicate","message_type":"notification","subscription_type":"channel.follow"},"payload":{"event":{"user_name":"Pyro"}}}
            """;
        var consumer = CreateConsumer(_notifier.Object);
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

        await HandleAsync(CreateConsumer(_notifier.Object), Envelope("notification", raw));

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
    public async Task RedemptionIsLeftForRewardConsumerToEnrich()
    {
        const string raw = """
            {"metadata":{"message_type":"notification","subscription_type":"channel.channel_points_custom_reward_redemption.add"},"payload":{"event":{"id":"redemption-1"}}}
            """;

        await HandleAsync(CreateConsumer(_notifier.Object), Envelope("notification", raw));

        Assert.Empty(_forwarded);
    }

    [Theory]
    [InlineData("session_welcome")]
    [InlineData("session_keepalive")]
    [InlineData("session_reconnect")]
    [InlineData("revocation")]
    public async Task NonNotificationFrameIsNotForwarded(string messageType)
    {
        const string raw = """{"metadata":{"subscription_type":"channel.follow"},"payload":{}}""";

        await HandleAsync(CreateConsumer(_notifier.Object), Envelope(messageType, raw));

        Assert.Empty(_forwarded);
    }

    private static TwitchEventSubConsumer CreateConsumer(ITelegramusNotifier notifier) =>
        new(
            Options.Create(new RabbitMqOptions()),
            notifier,
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
                [RabbitMqConfig.TwitchEventSubRaw, json, TestContext.Current.CancellationToken]
            )!;
    }

    private static string Envelope(string messageType, string raw)
    {
        using var document = JsonDocument.Parse(raw);

        return JsonSerializer.Serialize(
            new
            {
                schemaVersion = 1,
                messageId = "event-message-1",
                messageType,
                raw = document.RootElement,
            }
        );
    }
}
