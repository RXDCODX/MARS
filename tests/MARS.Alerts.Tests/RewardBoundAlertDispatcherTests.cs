using MARS.Alerts.Services.Alerts;
using MARS.Alerts.Services.RewardInput;
using MARS.Alerts.Services.TriggerWords;
using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Shared.Configuration;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Alerts.Tests;

/// <summary>
/// Показ алерта, привязанного к награде: по <c>MetaInfo.TwitchGuid</c> для
/// сообщения с текстом награды и по <c>MetaInfo.TwitchPointsCost</c> для
/// погашения награды без текста.
/// </summary>
public class RewardBoundAlertDispatcherTests
{
    private static MediaInfo Alert(Guid? twitchGuid = null, int pointsCost = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            TextInfo = new MediaTextInfo(),
            FileInfo = new MediaFileInfo
            {
                Type = MediaType.Video,
                FilePath = "alert.mp4",
                FileName = "alert.mp4",
                Extension = ".mp4",
            },
            PositionInfo = new MediaPositionInfo(),
            MetaInfo = new MediaMetaInfo
            {
                DisplayName = "alert",
                TwitchGuid = twitchGuid,
                TwitchPointsCost = pointsCost,
            },
            StylesInfo = new MediaStylesInfo(),
        };

    private static TwitchUser Viewer() =>
        new()
        {
            TwitchId = "42",
            UserLogin = "viewer",
            DisplayName = "Viewer",
            ChatColor = "#FF0000",
        };

    private static Mock<IEnabledAlertSource> Source(params MediaInfo[] alerts)
    {
        var source = new Mock<IEnabledAlertSource>();
        source
            .Setup(x => x.GetEnabledAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(alerts);

        return source;
    }

    private static Mock<ITelegramusNotifier> Notifier()
    {
        var notifier = new Mock<ITelegramusNotifier>();
        notifier.Setup(x => x.Alert(It.IsAny<MediaDto>())).Returns(Task.CompletedTask);

        return notifier;
    }

    private static RewardBoundAlertDispatcher Build(
        IEnabledAlertSource source,
        ITelegramusNotifier notifier,
        RickRollerService rickRoller
    ) => new(source, notifier, rickRoller, NullLogger<RewardBoundAlertDispatcher>.Instance);

    private static RickRollerService RickRoller(
        ITelegramusNotifier notifier,
        string? chance = null
    ) =>
        new(
            notifier,
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    chance is null
                        ? null
                        : new Dictionary<string, string?>
                        {
                            ["AppSettings:RickRoll:Chance"] = chance,
                        }
                )
                .Build()
        );

    [Fact]
    public async Task DispatchRewardInputAsync_ShowsTheAlertLinkedToTheReward()
    {
        var rewardId = Guid.NewGuid();
        var alert = Alert(twitchGuid: rewardId);
        var notifier = Notifier();
        var dispatcher = Build(Source(alert).Object, notifier.Object, RickRoller(notifier.Object));
        var ct = TestContext.Current.CancellationToken;

        await dispatcher.DispatchRewardInputAsync(
            new ChatMessageEvent
            {
                UserId = "42",
                UserName = "Viewer",
                Message = "текст награды",
                ChatColor = "#FF0000",
                CustomRewardId = rewardId.ToString(),
            },
            Viewer(),
            ct
        );

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Once);
    }

    /// <summary>
    /// Чужой GUID награды не должен поднимать чужой алерт: в монолите отбор шёл
    /// по точному совпадению <c>MetaInfo.TwitchGuid</c>.
    /// </summary>
    [Fact]
    public async Task DispatchRewardInputAsync_DoesNotShowAnAlertLinkedToAnotherReward()
    {
        var notifier = Notifier();
        var dispatcher = Build(
            Source(Alert(twitchGuid: Guid.NewGuid())).Object,
            notifier.Object,
            RickRoller(notifier.Object)
        );
        var ct = TestContext.Current.CancellationToken;

        await dispatcher.DispatchRewardInputAsync(
            new ChatMessageEvent
            {
                UserId = "42",
                UserName = "Viewer",
                Message = "текст награды",
                CustomRewardId = Guid.NewGuid().ToString(),
            },
            Viewer(),
            ct
        );

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Never);
    }

    /// <summary>
    /// Сообщение без <c>CustomRewardId</c> — обычный чат, обработчик возиться с
    /// ним не должен.
    /// </summary>
    [Fact]
    public async Task DispatchRewardInputAsync_DoesNothing_ForAPlainChatMessage()
    {
        var notifier = Notifier();
        var dispatcher = Build(
            Source(Alert(twitchGuid: Guid.NewGuid())).Object,
            notifier.Object,
            RickRoller(notifier.Object)
        );
        var ct = TestContext.Current.CancellationToken;

        await dispatcher.DispatchRewardInputAsync(
            new ChatMessageEvent
            {
                UserId = "42",
                UserName = "Viewer",
                Message = "привет",
            },
            Viewer(),
            ct
        );

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Never);
    }

    /// <summary>
    /// Монолит отбирал по <c>MetaInfo.TwitchPointsCost</c> — награда без привязки
    /// к конкретному <c>ChannelRewardRecord</c> всё равно поднимала алерт с
    /// такой же ценой.
    /// </summary>
    [Fact]
    public async Task DispatchRedemptionAsync_ShowsTheAlertWithTheSamePointsCost()
    {
        var notifier = Notifier();
        var dispatcher = Build(
            Source(Alert(pointsCost: 500)).Object,
            notifier.Object,
            RickRoller(notifier.Object)
        );
        var ct = TestContext.Current.CancellationToken;

        await dispatcher.DispatchRedemptionAsync(
            new RewardRedeemedEvent { Cost = 500, UserName = "Viewer" },
            Viewer(),
            ct
        );

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Once);
    }

    [Fact]
    public async Task DispatchRedemptionAsync_DoesNotShowAnAlertWithAnotherCost()
    {
        var notifier = Notifier();
        var dispatcher = Build(
            Source(Alert(pointsCost: 500)).Object,
            notifier.Object,
            RickRoller(notifier.Object)
        );
        var ct = TestContext.Current.CancellationToken;

        await dispatcher.DispatchRedemptionAsync(
            new RewardRedeemedEvent { Cost = 100 },
            Viewer(),
            ct
        );

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Never);
    }

    /// <summary>
    /// Цена 0 у бесплатных наград совпадает с <c>TwitchPointsCost = 0</c> у
    /// ненастроенных алертов: сравнивать их нельзя, иначе любая бесплатная
    /// награда поднимала бы случайный алерт.
    /// </summary>
    [Fact]
    public async Task DispatchRedemptionAsync_IgnoresAlertsWithoutAPointsCost()
    {
        var notifier = Notifier();
        var dispatcher = Build(
            Source(Alert(pointsCost: 0)).Object,
            notifier.Object,
            RickRoller(notifier.Object)
        );
        var ct = TestContext.Current.CancellationToken;

        await dispatcher.DispatchRedemptionAsync(
            new RewardRedeemedEvent { Cost = 0, UserName = "Viewer" },
            Viewer(),
            ct
        );

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Never);
    }

    /// <summary>
    /// Погашение с текстом пользователя уже разбирает <c>TwitchMediaAlerts</c> по
    /// привязанному медиа, поэтому подбор по цене дублировать нельзя.
    /// </summary>
    [Fact]
    public async Task DispatchRedemptionAsync_IgnoresRedemptionsWithUserInput()
    {
        var notifier = Notifier();
        var dispatcher = Build(
            Source(Alert(pointsCost: 500)).Object,
            notifier.Object,
            RickRoller(notifier.Object)
        );
        var ct = TestContext.Current.CancellationToken;

        await dispatcher.DispatchRedemptionAsync(
            new RewardRedeemedEvent
            {
                Cost = 500,
                UserName = "Viewer",
                UserInput = "что-то",
                Media = Alert(pointsCost: 500),
            },
            Viewer(),
            ct
        );

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Never);
    }

    /// <summary>
    /// Подбор по цене в монолите был обёрнут в рикролл: показывался либо
    /// случайный ролик, либо сам алерт. При всегда включённом рикролле
    /// показывается ролик, а не найденный по цене алерт.
    /// </summary>
    [Fact]
    public async Task DispatchRedemptionAsync_GoesThroughRickRoll()
    {
        var notifier = Notifier();
        var dispatcher = Build(
            Source(Alert(pointsCost: 500)).Object,
            notifier.Object,
            RickRoller(notifier.Object, chance: "1")
        );
        var ct = TestContext.Current.CancellationToken;
        var shown = new List<MediaDto>();
        notifier
            .Setup(x => x.Alert(It.IsAny<MediaDto>()))
            .Callback<MediaDto>(shown.Add)
            .Returns(Task.CompletedTask);

        await dispatcher.DispatchRedemptionAsync(
            new RewardRedeemedEvent { Cost = 500, UserName = "Viewer" },
            Viewer(),
            ct
        );

        var media = Assert.Single(shown).MediaInfo;
        Assert.Equal("Alerts/rickroll.mp4", media.FileInfo.FilePath);
    }

    /// <summary>
    /// Обычный случай: рикролл не выпал — показывается алерт, найденный по цене.
    /// </summary>
    [Fact]
    public async Task DispatchRedemptionAsync_ShowsTheMatchedAlert_WhenRickRollDidNotFire()
    {
        var alert = Alert(pointsCost: 500);
        var notifier = Notifier();
        var dispatcher = Build(
            Source(alert).Object,
            notifier.Object,
            RickRoller(notifier.Object, chance: "0")
        );
        var ct = TestContext.Current.CancellationToken;
        var shown = new List<MediaDto>();
        notifier
            .Setup(x => x.Alert(It.IsAny<MediaDto>()))
            .Callback<MediaDto>(shown.Add)
            .Returns(Task.CompletedTask);

        await dispatcher.DispatchRedemptionAsync(
            new RewardRedeemedEvent { Cost = 500, UserName = "Viewer" },
            Viewer(),
            ct
        );

        Assert.Equal(alert.Id, Assert.Single(shown).MediaInfo.Id);
    }
}
