using System.Reflection;
using System.Text.Json;
using MARS.Alerts.Services.Twitch.Rewards.Activation;
using MARS.Shared.Configuration;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using MARS.Shared.Models.Rewards;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Alerts.Tests.Services.Twitch.Rewards.Activation;

/// <summary>
/// Проверяет обработку редемпшенов и формирование единого JSON-события.
/// </summary>
public class RewardActivatedConsumerTests
{
    private readonly Mock<ITelegramusNotifier> _notifier = new();
    private readonly Mock<IRewardMediaResolver> _resolver = new();
    private string? _sentJson;

    public RewardActivatedConsumerTests()
    {
        _resolver
            .Setup(resolver =>
                resolver.ResolveAsync(
                    It.IsAny<RewardRedeemedEvent>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        _notifier
            .Setup(notifier => notifier.TwitchEventSub(It.IsAny<string>()))
            .Callback<string>(json => _sentJson = json)
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task RedemptionWithoutAlertKeyIsSentAsGenericEvent()
    {
        var consumer = CreateConsumer(chance: "0");

        await HandleAsync(consumer, Event(alertKey: null));

        _resolver.Verify(
            resolver =>
                resolver.ResolveAsync(
                    It.IsAny<RewardRedeemedEvent>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _notifier.Verify(notifier => notifier.TwitchEventSub(It.IsAny<string>()), Times.Once);

        using var document = ReadSentEvent();
        var root = document.RootElement;
        Assert.Equal("mars.reward.activated", root.GetProperty("type").GetString());
        var payload = root.GetProperty("payload");
        Assert.Equal("reward-1", payload.GetProperty("rewardId").GetString());
        Assert.Equal(
            "название больше не участвует в маршруте",
            payload.GetProperty("rewardTitle").GetString()
        );
        Assert.Equal(JsonValueKind.Null, payload.GetProperty("alertKey").ValueKind);
        Assert.Equal("Pyro", payload.GetProperty("user").GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task EffectRunsAndItsDataIsIncludedInJson()
    {
        var effect = new StubEffect(RewardAlertKeys.AdhdSuperpower)
        {
            Result = new RewardEffectResult { Data = new { Seconds = 60 } },
        };
        var consumer = CreateConsumer(chance: "0", effect);

        await HandleAsync(
            consumer,
            Event(alertKey: RewardAlertKeys.AdhdSuperpower, userInput: "hello", cost: 350)
        );

        Assert.Equal(1, effect.Calls);
        using var document = ReadSentEvent();
        var payload = document.RootElement.GetProperty("payload");
        Assert.Equal("hello", payload.GetProperty("userInput").GetString());
        Assert.Equal(350, payload.GetProperty("cost").GetInt32());
        Assert.Equal(60, payload.GetProperty("effectData").GetProperty("seconds").GetInt32());
    }

    [Fact]
    public async Task UnknownAlertKeyStillProducesGenericRewardEvent()
    {
        var consumer = CreateConsumer(chance: "0");

        await HandleAsync(consumer, Event(alertKey: "legacy-unknown-key"));

        _notifier.Verify(notifier => notifier.TwitchEventSub(It.IsAny<string>()), Times.Once);
        using var document = ReadSentEvent();
        Assert.Equal(
            "legacy-unknown-key",
            document.RootElement.GetProperty("payload").GetProperty("alertKey").GetString()
        );
    }

    [Fact]
    public async Task SuppressedEffectDoesNotSendOverlayEvent()
    {
        var effect = new StubEffect(RewardAlertKeys.CloseGame)
        {
            Result = RewardEffectResult.Suppressed,
        };
        var consumer = CreateConsumer(chance: "0", effect);

        await HandleAsync(consumer, Event(alertKey: RewardAlertKeys.CloseGame));

        _notifier.Verify(notifier => notifier.TwitchEventSub(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task MediaFromStorageIsPreparedForViewerAndSerialized()
    {
        var template = Media("{user.name}", "{user.color}");
        _resolver
            .Setup(resolver =>
                resolver.ResolveAsync(
                    It.IsAny<RewardRedeemedEvent>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([template]);

        var consumer = CreateConsumer(chance: "0");

        await HandleAsync(
            consumer,
            Event(alertKey: null, user: User(color: "#00FF00"), userInput: "текст")
        );

        using var document = ReadSentEvent();
        var mediaJson = document
            .RootElement.GetProperty("payload")
            .GetProperty("media")[0]
            .GetRawText();
        var media = JsonSerializer.Deserialize<MediaInfo>(
            mediaJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
            }
        );

        Assert.NotNull(media);
        Assert.NotSame(template, media);
        Assert.Equal("Pyro", media.TextInfo.Text);
        Assert.Equal("#00FF00", media.TextInfo.KeyWordsColor);
        Assert.Equal("alert", media.MetaInfo.DisplayName);
    }

    [Fact]
    public async Task UserIsRebuiltWhenEventHasNoUser()
    {
        var consumer = CreateConsumer(chance: "0");

        await HandleAsync(consumer, Event(alertKey: null, user: null));

        using var document = ReadSentEvent();
        var user = JsonSerializer.Deserialize<TwitchUser>(
            document.RootElement.GetProperty("payload").GetProperty("user").GetRawText(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
            }
        );

        Assert.NotNull(user);
        Assert.Equal("Pyro", user.DisplayName);
        Assert.Equal("Pyro", user.UserLogin);
        Assert.Equal("42", user.TwitchId);
    }

    [Fact]
    public async Task UnreadableBodyThrows()
    {
        var consumer = CreateConsumer(chance: "0");

        var exception = await Record.ExceptionAsync(() => HandleAsync(consumer, "null"));
        var inner = exception is TargetInvocationException invocation
            ? invocation.InnerException
            : exception;

        Assert.IsType<InvalidOperationException>(inner);
    }

    [Fact]
    public void DuplicateEffectsKeepFirst()
    {
        var first = new StubEffect(RewardAlertKeys.Confetti);
        var second = new StubEffect(RewardAlertKeys.Confetti);

        var map = RewardActivatedConsumer.BuildEffectMap([first, second], NullLogger.Instance);

        Assert.Same(first, map[RewardAlertKeys.Confetti]);
    }

    private JsonDocument ReadSentEvent()
    {
        Assert.False(string.IsNullOrWhiteSpace(_sentJson));
        return JsonDocument.Parse(_sentJson!);
    }

    private RewardActivatedConsumer CreateConsumer(
        string chance,
        params IRewardActivationEffect[] effects
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["AppSettings:RickRoll:Chance"] = chance }
            )
            .Build();

        return new RewardActivatedConsumer(
            Options.Create(new RabbitMqOptions()),
            effects,
            _resolver.Object,
            new RickRollerService(_notifier.Object, configuration),
            _notifier.Object,
            NullLogger<RewardActivatedConsumer>.Instance
        );
    }

    private static Task HandleAsync(RewardActivatedConsumer consumer, string json)
    {
        var method = typeof(RewardActivatedConsumer).GetMethod(
            "HandleMessageAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        return (Task)
            method.Invoke(
                consumer,
                [RabbitMqConfig.RewardActivated, json, TestContext.Current.CancellationToken]
            )!;
    }

    private static string Event(
        string? alertKey,
        string? userInput = null,
        int cost = 100,
        TwitchUser? user = null
    ) =>
        JsonSerializer.Serialize(
            new RewardRedeemedEvent
            {
                RewardId = "reward-1",
                RewardTitle = "название больше не участвует в маршруте",
                AlertKey = alertKey,
                UserId = "42",
                UserName = "Pyro",
                UserInput = userInput,
                Cost = cost,
                MessageId = "redemption-1",
                RedeemedAt = DateTime.Parse("2026-11-03T12:00:00Z").ToUniversalTime(),
                User = user,
            }
        );

    private static TwitchUser User(string? color = null) =>
        new()
        {
            TwitchId = "42",
            UserLogin = "pyro",
            DisplayName = "Pyro",
            ChatColor = color,
        };

    private static MediaInfo Media(string? text = null, string? color = null) =>
        new()
        {
            TextInfo = new MediaTextInfo { Text = text, KeyWordsColor = color },
            FileInfo = new MediaFileInfo
            {
                Type = MediaType.Video,
                FilePath = "alerts/alert.mp4",
                FileName = "alert.mp4",
                Extension = ".mp4",
            },
            PositionInfo = new MediaPositionInfo(),
            MetaInfo = new MediaMetaInfo { DisplayName = "alert" },
            StylesInfo = new MediaStylesInfo(),
        };

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
