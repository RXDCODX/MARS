using MARS.Alerts.Application.Alerts;
using MARS.Alerts.Application.RewardInput;
using MARS.Alerts.Application.TriggerWords;
using MARS.Alerts.Application.Twitch.Rewards;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests;

/// <summary>
/// Проверяет маршрутизацию обычного чата, reward-input и погашений наград.
/// </summary>
public class RewardInputConsumersTests
{
    private static RewardBoundAlertDispatcher CreateRewardBoundDispatcher(
        IEnabledAlertSource source
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["AppSettings:RickRoll:Chance"] = "-1" }
            )
            .Build();

        return new RewardBoundAlertDispatcher(
            source,
            new RickRollerService(configuration),
            NullLogger<RewardBoundAlertDispatcher>.Instance
        );
    }

    private static Mock<IEnabledAlertSource> CreateSource()
    {
        var source = new Mock<IEnabledAlertSource>();
        source
            .Setup(x => x.GetEnabledAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<MediaInfo>?)Array.Empty<MediaInfo>());

        return source;
    }

    [Fact]
    public async Task RewardInputMessageListener_DispatchesMessageWithRewardId()
    {
        var source = CreateSource();
        var listener = new RewardInputMessageListener(CreateRewardBoundDispatcher(source.Object));
        var rewardId = Guid.NewGuid();
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

        source.Verify(x => x.GetEnabledAlertsAsync(ct), Times.Once);
    }

    [Fact]
    public async Task RewardInputMessageListener_IgnoresPlainChatMessages()
    {
        var source = CreateSource();
        var listener = new RewardInputMessageListener(CreateRewardBoundDispatcher(source.Object));
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

        source.Verify(x => x.GetEnabledAlertsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TriggerWordRewardInputListener_DispatchesNonEmptyMessage()
    {
        var source = CreateSource();
        var listener = new TriggerWordRewardInputListener(
            new TriggerWordAlertDispatcher(
                source.Object,
                NullLogger<TriggerWordAlertDispatcher>.Instance
            )
        );
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
    public async Task TriggerWordRewardInputListener_IgnoresEmptyMessage()
    {
        var source = CreateSource();
        var listener = new TriggerWordRewardInputListener(
            new TriggerWordAlertDispatcher(
                source.Object,
                NullLogger<TriggerWordAlertDispatcher>.Instance
            )
        );
        var ct = TestContext.Current.CancellationToken;

        await listener.Handle(
            new RewardInputChatMessageEvent
            {
                Message = new ChatEventMessageEvent
                {
                    UserId = "42",
                    UserName = "Viewer",
                    Message = " ",
                },
            },
            ct
        );

        source.Verify(x => x.GetEnabledAlertsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CostMatchedRewardListener_DispatchesRedemptionWithoutUserInput()
    {
        var source = CreateSource();
        var listener = new CostMatchedRewardListener(CreateRewardBoundDispatcher(source.Object));
        var ct = TestContext.Current.CancellationToken;

        await listener.Handle(
            new RewardRedeemedEvent
            {
                Cost = 500,
                UserId = "42",
                UserName = "Viewer",
            },
            ct
        );

        source.Verify(x => x.GetEnabledAlertsAsync(ct), Times.Once);
    }

    [Fact]
    public async Task CostMatchedRewardListener_SkipsRedemptionWithUserInput()
    {
        var source = CreateSource();
        var listener = new CostMatchedRewardListener(CreateRewardBoundDispatcher(source.Object));
        var ct = TestContext.Current.CancellationToken;

        await listener.Handle(
            new RewardRedeemedEvent
            {
                Cost = 500,
                UserId = "42",
                UserName = "Viewer",
                UserInput = "message already handled by the reward listener",
            },
            ct
        );

        source.Verify(x => x.GetEnabledAlertsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TwitchRewardInputListener_ForwardsMessageToSubscribedRewardHandlers()
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

    private sealed class RewardInputProbeHandler : RewardHandlerBase
    {
        public RewardInputProbeHandler()
        {
            OnTwitchRewardInput += HandleRewardInputAsync;
        }

        public override string RewardKey => "reward-input-probe";

        public List<ChatEventMessageEvent> Messages { get; } = [];

        protected override Task OnRewardRedeemedAsync(
            RewardRedeemedEvent rewardEvent,
            CancellationToken ct
        ) => Task.CompletedTask;

        private Task HandleRewardInputAsync(ChatEventMessageEvent message, CancellationToken ct)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
