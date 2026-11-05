using MARS.Alerts.Services.Alerts;
using MARS.Alerts.Services.RewardInput;
using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests;

/// <summary>
/// Маршрутизация сообщений о наградах в диспетчер AC.S08.
/// </summary>
public class RewardInputListenersTests
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

    private static Mock<ITelegramusNotifier> Build(
        Mock<IEnabledAlertSource> source,
        params MediaInfo[] alerts
    )
    {
        source
            .Setup(x => x.GetEnabledAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(alerts);

        var notifier = new Mock<ITelegramusNotifier>();
        notifier.Setup(x => x.Alert(It.IsAny<MediaDto>())).Returns(Task.CompletedTask);

        return notifier;
    }

    private static RewardBoundAlertDispatcher Dispatcher(
        IEnabledAlertSource source,
        ITelegramusNotifier notifier
    ) =>
        new(
            source,
            notifier,
            new RickRollerService(notifier, new ConfigurationBuilder().Build()),
            NullLogger<RewardBoundAlertDispatcher>.Instance
        );

    [Fact]
    public async Task RewardInputMessageListener_ShowsTheAlertBoundToTheReward()
    {
        var rewardId = Guid.NewGuid();
        var source = new Mock<IEnabledAlertSource>();
        var notifier = Build(source, Alert(twitchGuid: rewardId));
        var listener = new RewardInputMessageListener(Dispatcher(source.Object, notifier.Object));
        var message = new RewardInputMessageEvent
        {
            UserId = "42",
            UserName = "Viewer",
            Message = "текст награды",
            CustomRewardId = rewardId.ToString(),
        };

        await listener.Handle(message, TestContext.Current.CancellationToken);

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Once);
    }

    [Fact]
    public async Task CostMatchedRewardListener_ShowsTheAlertWithTheSameCost()
    {
        var source = new Mock<IEnabledAlertSource>();
        var notifier = Build(source, Alert(pointsCost: 500));
        var listener = new CostMatchedRewardListener(Dispatcher(source.Object, notifier.Object));
        var message = new RewardRedeemedEvent
        {
            Cost = 500,
            UserName = "Viewer",
            UserId = "42",
            RewardId = "reward",
        };

        await listener.Handle(message, TestContext.Current.CancellationToken);

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Once);
    }
}
