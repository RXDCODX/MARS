using System.Text.Json;
using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Alerts.Services.Twitch.Rewards.Activation;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using MARS.Shared.Models.Rewards;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests.Services.Twitch.Rewards.Activation;

public class RewardRedemptionProcessorTests
{
    private readonly Mock<ITelegramusNotifier> _notifier = new();
    private readonly Mock<IRewardMediaResolver> _resolver = new();

    public RewardRedemptionProcessorTests()
    {
        _resolver
            .Setup(resolver =>
                resolver.ResolveAsync(
                    It.IsAny<RewardRedeemedEvent>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task RedemptionWithoutAlertKeyKeepsRawTwitchEventAndAddsGenericMarsExtension()
    {
        const string raw = """
            {
              "metadata": {
                "message_id": "eventsub-1",
                "message_type": "notification",
                "subscription_type": "channel.channel_points_custom_reward_redemption.add"
              },
              "payload": {
                "event": {
                  "id": "redemption-1",
                  "user_id": "42",
                  "user_login": "pyro",
                  "user_name": "Pyro",
                  "user_input": "hello",
                  "redeemed_at": "2026-11-03T12:00:00Z",
                  "reward": { "id": "reward-1", "title": "Новая награда", "cost": 100 }
                }
              }
            }
            """;

        var processor = CreateProcessor();
        var result = await processor.ProcessAsync(raw, null, null, CancellationToken.None);

        using var document = JsonDocument.Parse(Assert.IsType<string>(result));
        var root = document.RootElement;

        Assert.Equal(
            "eventsub-1",
            root.GetProperty("metadata").GetProperty("message_id").GetString()
        );
        Assert.Equal(
            "redemption-1",
            root.GetProperty("payload").GetProperty("event").GetProperty("id").GetString()
        );

        var marsReward = root.GetProperty("mars").GetProperty("reward");
        Assert.Equal("mars.reward.activated", marsReward.GetProperty("type").GetString());
        Assert.Equal("eventsub-1", marsReward.GetProperty("eventId").GetString());

        var payload = marsReward.GetProperty("payload");
        Assert.Equal("reward-1", payload.GetProperty("rewardId").GetString());
        Assert.Equal("Новая награда", payload.GetProperty("rewardTitle").GetString());
        Assert.Equal(100, payload.GetProperty("cost").GetInt32());
        Assert.Equal("hello", payload.GetProperty("userInput").GetString());
        Assert.Equal("Pyro", payload.GetProperty("user").GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task SpecialHandlerRunsAndItsDataIsAttachedToMarsExtension()
    {
        var effect = new StubEffect(RewardAlertKeys.AdhdSuperpower)
        {
            Result = new RewardEffectResult { Data = new { seconds = 60 } },
        };
        var processor = CreateProcessor(effect);

        var result = await processor.ProcessAsync(
            EventSub(alertKey: RewardAlertKeys.AdhdSuperpower),
            RewardAlertKeys.AdhdSuperpower,
            null,
            CancellationToken.None
        );

        Assert.Equal(1, effect.Calls);
        using var document = JsonDocument.Parse(Assert.IsType<string>(result));
        var reward = document.RootElement.GetProperty("mars").GetProperty("reward");
        Assert.Equal(
            RewardAlertKeys.AdhdSuperpower,
            reward.GetProperty("payload").GetProperty("alertKey").GetString()
        );
        Assert.Equal(
            60,
            reward
                .GetProperty("payload")
                .GetProperty("effectData")
                .GetProperty("seconds")
                .GetInt32()
        );
    }

    [Fact]
    public async Task UnknownAlertKeyDoesNotPreventGenericRedemption()
    {
        var processor = CreateProcessor();

        var result = await processor.ProcessAsync(
            EventSub("legacy-unknown-key"),
            "legacy-unknown-key",
            null,
            CancellationToken.None
        );

        Assert.NotNull(result);
        using var document = JsonDocument.Parse(result);
        Assert.Equal(
            "mars.reward.activated",
            document
                .RootElement.GetProperty("mars")
                .GetProperty("reward")
                .GetProperty("type")
                .GetString()
        );
    }

    [Fact]
    public async Task SuppressedSpecialEffectReturnsNull()
    {
        var effect = new StubEffect(RewardAlertKeys.CloseGame)
        {
            Result = RewardEffectResult.Suppressed,
        };
        var processor = CreateProcessor(effect);

        var result = await processor.ProcessAsync(
            EventSub(RewardAlertKeys.CloseGame),
            RewardAlertKeys.CloseGame,
            null,
            CancellationToken.None
        );

        Assert.Null(result);
        Assert.Equal(1, effect.Calls);
    }

    [Fact]
    public async Task DuplicateEventSubMessageDoesNotRunSpecialEffectTwice()
    {
        var effect = new StubEffect(RewardAlertKeys.AdhdSuperpower)
        {
            Result = new RewardEffectResult { Data = new { seconds = 60 } },
        };
        var processor = CreateProcessor(effect);
        var raw = EventSub(RewardAlertKeys.AdhdSuperpower);

        var first = await processor.ProcessAsync(
            raw,
            RewardAlertKeys.AdhdSuperpower,
            null,
            CancellationToken.None
        );
        var second = await processor.ProcessAsync(
            raw,
            RewardAlertKeys.AdhdSuperpower,
            null,
            CancellationToken.None
        );

        Assert.Equal(first, second);
        Assert.Equal(1, effect.Calls);
    }

    [Fact]
    public async Task LocalMediaInfoIdIsPassedToMediaResolver()
    {
        var mediaInfoId = Guid.NewGuid();
        var processor = CreateProcessor();

        await processor.ProcessAsync(EventSub(), null, mediaInfoId, CancellationToken.None);

        _resolver.Verify(
            resolver =>
                resolver.ResolveAsync(
                    It.Is<RewardRedeemedEvent>(reward =>
                        reward.MediaInfoId == mediaInfoId
                        && reward.RewardId == "reward-1"
                        && reward.MessageId == "redemption-1"
                        && reward.EventSubMessageId == "eventsub-1"
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task MissingRewardUserStillProducesFallbackUser()
    {
        const string raw = """
            {
              "metadata": { "message_id": "eventsub-no-user" },
              "payload": {
                "event": {
                  "id": "redemption-no-user",
                  "reward": { "id": "reward-1", "title": "No user", "cost": 1 }
                }
              }
            }
            """;

        var result = await CreateProcessor().ProcessAsync(raw, null, null, CancellationToken.None);

        using var document = JsonDocument.Parse(Assert.IsType<string>(result));
        Assert.Equal(
            "Зритель",
            document
                .RootElement.GetProperty("mars")
                .GetProperty("reward")
                .GetProperty("payload")
                .GetProperty("user")
                .GetProperty("displayName")
                .GetString()
        );
    }

    [Fact]
    public async Task InvalidEventSubBodyThrows()
    {
        var processor = CreateProcessor();

        await Assert.ThrowsAsync<JsonException>(() =>
            processor.ProcessAsync("{invalid", null, null, CancellationToken.None)
        );
    }

    private RewardRedemptionProcessor CreateProcessor(params IRewardActivationEffect[] effects)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["AppSettings:RickRoll:Chance"] = "0" }
            )
            .Build();

        return new RewardRedemptionProcessor(
            effects,
            _resolver.Object,
            new RickRollerService(_notifier.Object, configuration),
            NullLogger<RewardRedemptionProcessor>.Instance
        );
    }

    private static string EventSub(string? alertKey = null)
    {
        var json = """
            {
              "metadata": {
                "message_id": "eventsub-1",
                "message_type": "notification",
                "subscription_type": "channel.channel_points_custom_reward_redemption.add"
              },
              "payload": {
                "event": {
                  "id": "redemption-1",
                  "user_id": "42",
                  "user_login": "pyro",
                  "user_name": "Pyro",
                  "user_input": "hello",
                  "redeemed_at": "2026-11-03T12:00:00Z",
                  "reward": { "id": "reward-1", "title": "Test reward", "cost": 100 }
                }
              }
            }
            """;

        return json;
    }

    private sealed class StubEffect(string alertKey) : IRewardActivationEffect
    {
        public string AlertKey { get; } = alertKey;
        public RewardEffectResult Result { get; set; } = RewardEffectResult.Passthrough;
        public int Calls { get; private set; }

        public Task<RewardEffectResult> ApplyAsync(
            RewardActivationContext context,
            CancellationToken cancellationToken
        )
        {
            Calls++;
            return Task.FromResult(Result);
        }
    }
}
