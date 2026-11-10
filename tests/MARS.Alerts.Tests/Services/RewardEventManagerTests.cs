using MARS.Alerts.Application.Twitch.Rewards;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Менеджер маршрутизирует редемпшены к одной награде, а общие события Twitch
/// рассылает подписчикам всех зарегистрированных обработчиков.
/// </summary>
public class RewardEventManagerTests
{
    [Fact]
    public async Task DispatchesTwitchMessagesToSeparateEvents()
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
    public async Task DispatchesEventSubToEverySubscribedRewardHandler()
    {
        var first = new ProbeHandler();
        var second = new ProbeHandler();
        var manager = Create(first, second);
        var twitchEvent = new EventSubRawEvent
        {
            EventName = "channel.follow",
            SubscriptionType = "channel.follow",
            MessageId = "event-1",
        };

        await manager.HandleTwitchEventAsync(twitchEvent, CancellationToken.None);

        Assert.Same(twitchEvent, Assert.Single(first.TwitchEvents));
        Assert.Same(twitchEvent, Assert.Single(second.TwitchEvents));
    }

    [Fact]
    public async Task AwaitsEverySubscriptionOnTheSameRewardHandler()
    {
        var handler = new ProbeHandler();
        var callbackCompleted = false;
        handler.OnTwitchMessage += async (_, _) =>
        {
            await Task.Yield();
            callbackCompleted = true;
        };
        var manager = Create(handler);

        await manager.HandleTwitchChatMessageAsync(
            new ChatEventMessageEvent { UserId = "42" },
            CancellationToken.None
        );

        Assert.True(callbackCompleted);
    }

    [Fact]
    public async Task DefaultEventsCompleteWhenRewardDoesNotSubscribeToThem()
    {
        var manager = Create(new DefaultHookHandler());
        var chatMessage = new ChatEventMessageEvent
        {
            UserId = "42",
            UserName = "viewer",
            Message = "message without a subscriber",
        };
        var ct = TestContext.Current.CancellationToken;

        await manager.HandleTwitchChatMessageAsync(chatMessage, ct);
        await manager.HandleTwitchRewardInputAsync(
            new RewardInputChatMessageEvent { Message = chatMessage },
            ct
        );
        await manager.HandleTwitchEventAsync(new EventSubRawEvent(), ct);
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
        public ProbeHandler()
        {
            OnTwitchMessage += HandleChatMessageAsync;
            OnTwitchRewardInput += HandleRewardInputAsync;
            OnTwitchEvent += HandleTwitchEventAsync;
        }

        public override string RewardKey => "probe";

        public List<ChatEventMessageEvent> ChatMessages { get; } = [];
        public List<ChatEventMessageEvent> RewardInputMessages { get; } = [];
        public List<EventSubRawEvent> TwitchEvents { get; } = [];
        public RewardRedeemedEvent? RedeemedEvent { get; private set; }

        protected override Task OnRewardRedeemedAsync(
            RewardRedeemedEvent rewardEvent,
            CancellationToken ct
        )
        {
            RedeemedEvent = rewardEvent;
            return Task.CompletedTask;
        }

        private Task HandleChatMessageAsync(ChatEventMessageEvent message, CancellationToken ct)
        {
            ChatMessages.Add(message);
            return Task.CompletedTask;
        }

        private Task HandleRewardInputAsync(ChatEventMessageEvent message, CancellationToken ct)
        {
            RewardInputMessages.Add(message);
            return Task.CompletedTask;
        }

        private Task HandleTwitchEventAsync(EventSubRawEvent twitchEvent, CancellationToken ct)
        {
            TwitchEvents.Add(twitchEvent);
            return Task.CompletedTask;
        }
    }
}
