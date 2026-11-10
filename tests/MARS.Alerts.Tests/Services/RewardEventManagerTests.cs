using MARS.Alerts.Application.Twitch.Rewards;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Менеджер разводит обычный чат, reward-input и погашение награды по разным
/// hooks базового класса: награда может подписаться только на нужные ей события.
/// </summary>
public class RewardEventManagerTests
{
    [Fact]
    public async Task DispatchesTwitchMessagesToSeparateHooks()
    {
        var handler = new ProbeHandler();
        var manager = Create(handler);
        var chatMessage = new ChatEventMessageEvent
        {
            UserId = "42",
            UserName = "viewer",
            Message = "ordinary chat",
        };
        var rewardInputMessage = new ChatEventMessageEvent
        {
            UserId = "43",
            UserName = "reward-viewer",
            Message = "reward input",
            CustomRewardId = "reward-1",
        };

        await manager.HandleTwitchChatMessageAsync(chatMessage, CancellationToken.None);

        Assert.Same(chatMessage, Assert.Single(handler.ChatMessages));
        Assert.Empty(handler.RewardInputMessages);

        await manager.HandleTwitchRewardInputAsync(
            new RewardInputChatMessageEvent { Message = rewardInputMessage },
            CancellationToken.None
        );

        Assert.Same(rewardInputMessage, Assert.Single(handler.RewardInputMessages));
        Assert.Single(handler.ChatMessages);
    }

    [Fact]
    public async Task DefaultChatHooksCompleteWhenRewardDoesNotSubscribeToThem()
    {
        var manager = Create(new DefaultHookHandler());
        var chatMessage = new ChatEventMessageEvent
        {
            UserId = "42",
            UserName = "viewer",
            Message = "message without a custom hook",
        };
        var ct = TestContext.Current.CancellationToken;

        await manager.HandleTwitchChatMessageAsync(chatMessage, ct);
        await manager.HandleTwitchRewardInputAsync(
            new RewardInputChatMessageEvent { Message = chatMessage },
            ct
        );
    }

    [Fact]
    public async Task RedeemedRewardUsesBaseEntryPoint()
    {
        var handler = new ProbeHandler();
        var manager = Create(handler);
        var rewardEvent = new RewardRedeemedEvent { RewardTitle = "Probe", UserName = "viewer" };

        await manager.HandleRewardRedeemedAsync(rewardEvent, CancellationToken.None);

        Assert.Same(rewardEvent, handler.RedeemedEvent);
    }

    private static RewardEventManager Create(params RewardHandlerBase[] handlers) =>
        new(handlers, NullLogger<RewardEventManager>.Instance);

    private sealed class DefaultHookHandler : RewardHandlerBase
    {
        public override string RewardKey => "default-hooks";

        protected override Task OnRewardRedeemedAsync(
            RewardRedeemedEvent rewardEvent,
            CancellationToken ct
        ) => Task.CompletedTask;
    }

    private sealed class ProbeHandler : RewardHandlerBase
    {
        public override string RewardKey => "probe";

        public List<ChatEventMessageEvent> ChatMessages { get; } = [];
        public List<ChatEventMessageEvent> RewardInputMessages { get; } = [];
        public RewardRedeemedEvent? RedeemedEvent { get; private set; }

        protected override Task OnRewardRedeemedAsync(
            RewardRedeemedEvent rewardEvent,
            CancellationToken ct
        )
        {
            RedeemedEvent = rewardEvent;
            return Task.CompletedTask;
        }

        protected override Task OnTwitchChatMessageAsync(
            ChatEventMessageEvent message,
            CancellationToken ct
        )
        {
            ChatMessages.Add(message);
            return Task.CompletedTask;
        }

        protected override Task OnTwitchRewardInputAsync(
            ChatEventMessageEvent message,
            CancellationToken ct
        )
        {
            RewardInputMessages.Add(message);
            return Task.CompletedTask;
        }
    }
}
