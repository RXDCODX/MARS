using System.Text.Json;
using MARS.Alerts.Application.Alerts;
using MARS.Alerts.Application.RewardInput;
using MARS.Alerts.Application.TriggerWords;
using MARS.Alerts.Application.Twitch.Rewards;
using MARS.Platform.Infrastructure.Grpc.Notifications;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests;

/// <summary>
/// Маршрутизация сообщений о наградах в диспетчер AC.S08.
/// </summary>
public class RewardInputConsumersTests
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
        var ct = TestContext.Current.CancellationToken;

        await listener.Handle(
            new RewardInputChatMessageEvent
            {
                Message = new ChatEventMessageEvent
                {
                    UserId = "42",
                    UserName = "Viewer",
                    Message = "текст награды",
                    CustomRewardId = rewardId.ToString(),
                },
            },
            ct
        );

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Once);
    }

    /// <summary>
    /// На своей очереди слушатель не должен реагировать на обычное сообщение
    /// чата: это делает <c>TriggerWordChatListener</c>.
    /// </summary>
    [Fact]
    public async Task RewardInputMessageListener_IgnoresPlainChatMessages()
    {
        var source = new Mock<IEnabledAlertSource>();
        var notifier = Build(source, Alert(twitchGuid: Guid.NewGuid()));
        var listener = new RewardInputMessageListener(Dispatcher(source.Object, notifier.Object));
        var ct = TestContext.Current.CancellationToken;

        await listener.Handle(
            new RewardInputChatMessageEvent
            {
                Message = new ChatEventMessageEvent
                {
                    UserId = "42",
                    UserName = "Viewer",
                    Message = "привет",
                },
            },
            ct
        );

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Never);
    }

    [Fact]
    public async Task TriggerWordRewardInputListener_ForwardsRewardInputToDispatcher()
    {
        var source = new Mock<IEnabledAlertSource>();
        source.Setup(x => x.GetEnabledAlertsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var notifier = new Mock<ITelegramusNotifier>();
        var dispatcher = new TriggerWordAlertDispatcher(
            source.Object,
            notifier.Object,
            NullLogger<TriggerWordAlertDispatcher>.Instance
        );
        var listener = new TriggerWordRewardInputListener(dispatcher);
        var ct = TestContext.Current.CancellationToken;

        await listener.Handle(
            new RewardInputChatMessageEvent
            {
                Message = new ChatEventMessageEvent
                {
                    UserId = "42",
                    UserName = "Viewer",
                    Message = "текст награды",
                },
            },
            ct
        );

        source.Verify(x => x.GetEnabledAlertsAsync(ct), Times.Once);
    }

    [Fact]
    public async Task TwitchRewardInputListener_ForwardsMessageToRewardHooks()
    {
        var handler = new RewardInputProbeHandler();
        var manager = new RewardEventManager([handler], NullLogger<RewardEventManager>.Instance);
        var listener = new TwitchRewardInputListener(
            manager,
            NullLogger<TwitchRewardInputListener>.Instance
        );
        var message = new ChatEventMessageEvent
        {
            UserId = "42",
            UserName = "Viewer",
            Message = "текст награды",
            CustomRewardId = "reward-1",
        };

        await listener.Handle(
            new RewardInputChatMessageEvent { Message = message },
            TestContext.Current.CancellationToken
        );

        Assert.Same(message, Assert.Single(handler.Messages));
    }

    [Fact]
    public async Task CostMatchedRewardListener_ShowsTheAlertWithTheSameCost()
    {
        var source = new Mock<IEnabledAlertSource>();
        var notifier = Build(source, Alert(pointsCost: 500));
        var listener = new CostMatchedRewardListener(Dispatcher(source.Object, notifier.Object));
        var ct = TestContext.Current.CancellationToken;

        await listener.Handle(
            new RewardRedeemedEvent
            {
                Cost = 500,
                UserName = "Viewer",
                UserId = "42",
                RewardId = "reward",
            },
            ct
        );

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Once);
    }

    private sealed class RewardInputProbeHandler : RewardHandlerBase
    {
        public override string RewardKey => "reward-input-probe";

        public List<ChatEventMessageEvent> Messages { get; } = [];

        protected override Task OnRewardRedeemedAsync(
            RewardRedeemedEvent rewardEvent,
            CancellationToken ct
        ) => Task.CompletedTask;

        protected override Task OnTwitchRewardInputAsync(
            ChatEventMessageEvent message,
            CancellationToken ct
        )
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
