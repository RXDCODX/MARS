using MARS.Alerts.Application.TwitchRewards;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Менеджер маршрутизирует редемпшены к одной награде, а общие события Twitch
/// рассылаются подписчикам всех зарегистрированных обработчиков.
/// </summary>
public class RewardEventManagerTests
{
    [Fact]
    public async Task DispatchesTwitchMessagesToSeparateEvents()
    {
        var manager = new RewardEventManager();
        var handler = new ProbeHandler(manager);
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
        var manager = new RewardEventManager();
        var first = new ProbeHandler(manager);
        var second = new ProbeHandler(manager);
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
        var manager = new RewardEventManager();
        var handler = new ProbeHandler(manager);
        var callbackCompleted = false;
        manager.OnTwitchMessage += async (_, _) =>
        {
            await Task.Yield();
            callbackCompleted = true;
        };

        await manager.HandleTwitchChatMessageAsync(
            new ChatEventMessageEvent { UserId = "42" },
            CancellationToken.None
        );

        Assert.True(callbackCompleted);
    }

    [Fact]
    public async Task DefaultEventsCompleteWhenRewardDoesNotSubscribeToThem()
    {
        var manager = new RewardEventManager();
        var handler = new DefaultHookHandler();
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
        var manager = new RewardEventManager();
        var handler = new ProbeHandler(manager);
        var rewardEvent = new RewardRedeemedEvent { RewardCode = "probe", UserName = "viewer" };

        await manager.HandleRewardRedeemedAsync(rewardEvent, CancellationToken.None);

        Assert.Same(rewardEvent, handler.RedeemedEvent);
    }

    private sealed class DefaultHookHandler : RewardHandlerBase
    {
        public override Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected override Task OnRewardRedeemedAsync(
            RewardRedeemedEvent rewardEvent,
            CancellationToken ct
        ) => Task.CompletedTask;
    }

    private sealed class ProbeHandler : RewardHandlerBase
    {
        public ProbeHandler(IRewardEventManager manager)
        {
            manager.OnTwitchMessage += HandleChatMessageAsync;
            manager.OnTwitchRewardInput += HandleRewardInputAsync;
            manager.OnTwitchEvent += HandleTwitchEventAsync;
            manager.OnTwitchRewardRedeem += HandleAsync;
        }

        public List<ChatEventMessageEvent> ChatMessages { get; } = [];
        public List<ChatEventMessageEvent> RewardInputMessages { get; } = [];
        public List<EventSubRawEvent> TwitchEvents { get; } = [];
        public RewardRedeemedEvent? RedeemedEvent { get; private set; }

        public override Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected override Task OnRewardRedeemedAsync(
            RewardRedeemedEvent rewardEvent,
            CancellationToken ct
        )
        {
            RedeemedEvent = rewardEvent;
            return Task.CompletedTask;
        }

        public Task HandleChatMessageAsync(ChatEventMessageEvent message, CancellationToken ct)
        {
            ChatMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task HandleRewardInputAsync(
            RewardInputChatMessageEvent message,
            CancellationToken ct
        )
        {
            RewardInputMessages.Add(message.Message);
            return Task.CompletedTask;
        }

        public Task HandleTwitchEventAsync(EventSubRawEvent twitchEvent, CancellationToken ct)
        {
            TwitchEvents.Add(twitchEvent);
            return Task.CompletedTask;
        }
    }
}
