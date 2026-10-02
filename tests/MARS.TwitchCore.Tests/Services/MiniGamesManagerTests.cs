using MARS.TwitchCore.Entities.Interfaces;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.Rewards;
using MARS.TwitchCore.Services.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Client.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Мини-игры, запускаемые наградами канала.
///
/// Игра одна: вторая награда во время игры либо добавляет участника (если игра
/// это умеет), либо получает отказ. Проверяется именно это решение — иначе
/// зритель, оплативший награду, не получил бы ничего.
/// </summary>
public class MiniGamesManagerTests
{
    [Fact]
    public async Task RedemptionStartsGame()
    {
        var game = new StubGame(cost: 100);
        var manager = Create(game);

        await InvokeRedemptionAsync(manager, cost: 100);

        Assert.Equal(1, game.Starts);
        Assert.True(game.IsGameRunning);
    }

    [Fact]
    public async Task UnknownCostIsIgnored()
    {
        var game = new StubGame(cost: 100);
        var manager = Create(game);

        await InvokeRedemptionAsync(manager, cost: 999);

        Assert.Equal(0, game.Starts);
    }

    /// <summary>
    /// Игра, умеющая принимать участников, получает нового игрока, а не отказ:
    /// награда уже оплачена.
    /// </summary>
    [Fact]
    public async Task ReusableRewardAddsPlayerToRunningGame()
    {
        var game = new StubGame(cost: 100) { IsReuseRewardForAddMechanic = true };
        var manager = Create(game);
        await InvokeRedemptionAsync(manager, cost: 100);

        await InvokeRedemptionAsync(manager, cost: 100);

        Assert.Equal(1, game.Starts);
        Assert.Equal(1, game.RedeemCalls);
    }

    [Fact]
    public async Task NonReusableRewardIsRefusedWhileGameRuns()
    {
        var game = new StubGame(cost: 100);
        var client = new Mock<ITwitchClient>();
        var manager = Create(game, client.Object);
        await InvokeRedemptionAsync(manager, cost: 100);

        await InvokeRedemptionAsync(manager, cost: 100);

        Assert.Equal(1, game.Starts);
        Assert.Equal(0, game.RedeemCalls);
    }

    [Fact]
    public async Task CompletedGameIsRemovedBeforeNextRedemption()
    {
        var game = new StubGame(cost: 100);
        var manager = Create(game);
        await InvokeRedemptionAsync(manager, cost: 100);

        game.IsGameRunning = false;
        await InvokeRedemptionAsync(manager, cost: 100);

        Assert.Equal(2, game.Starts);
    }

    [Fact]
    public async Task InactiveServiceIgnoresRedemption()
    {
        var game = new StubGame(cost: 100);
        var manager = new MiniGamesManager(
            [game],
            new TestLifetime(),
            Mock.Of<ITwitchClient>(),
            OfflineEventSub.Create(),
            PassingValidationService.Rejecting("Сервис временно неактивен")
        )
        {
            IsServiceActive = false,
        };

        await InvokeRedemptionAsync(manager, cost: 100);

        Assert.Equal(0, game.Starts);
    }

    [Fact]
    public async Task CancellingStopsRunningGameOnly()
    {
        var running = new StubGame(cost: 100);
        var idle = new StubGame(cost: 200);
        var manager = Create([running, idle]);
        await InvokeRedemptionAsync(manager, cost: 100);

        await manager.CancelAllGamesAsync();

        Assert.Equal(1, running.Cancels);
        Assert.Equal(0, idle.Cancels);
    }

    [Fact]
    public async Task ChatMessageReachesRunningGame()
    {
        var game = new StubGame(cost: 100);
        var manager = Create(game);
        await InvokeRedemptionAsync(manager, cost: 100);

        await InvokeMessageAsync(manager, "игра");

        Assert.Equal(["игра"], game.Messages);
    }

    [Fact]
    public async Task ChatMessageOfInactiveServiceIsIgnored()
    {
        var game = new StubGame(cost: 100);
        var manager = Create(game);
        manager.IsServiceActive = false;

        await InvokeMessageAsync(manager, "игра");

        Assert.Empty(game.Messages);
    }

    [Fact]
    public async Task StartAndStopHookUpEventSub()
    {
        var manager = Create(new StubGame(cost: 100));

        await manager.StartAsync(TestContext.Current.CancellationToken);
        await manager.StopAsync(TestContext.Current.CancellationToken);
    }

    private static MiniGamesManager Create(ITwitchMiniGame game, ITwitchClient? client = null) =>
        Create([game], client);

    private static MiniGamesManager Create(
        IEnumerable<ITwitchMiniGame> games,
        ITwitchClient? client = null
    ) =>
        new(
            games,
            new TestLifetime(),
            client ?? Mock.Of<ITwitchClient>(),
            OfflineEventSub.Create(),
            PassingValidationService.Instance
        );

    /// <summary>
    /// Обработчик награды приватный и подписан на событие EventSub, поэтому он
    /// вызывается напрямую — так же, как его зовёт Twitch.
    /// </summary>
    private static async Task InvokeRedemptionAsync(MiniGamesManager manager, int cost)
    {
        var method = typeof(MiniGamesManager).GetMethod(
            "WsClientOnChannelPointsCustomRewardRedemptionAdd",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        )!;

        var task = (Task)method.Invoke(manager, [null, Redemption(cost)])!;
        await task;
    }

    private static async Task InvokeMessageAsync(MiniGamesManager manager, string message)
    {
        var method = typeof(MiniGamesManager).GetMethod(
            "ClientOnMessageReceived",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        )!;

        var task = (Task)method.Invoke(manager, [null, PassingValidationService.Message(message)])!;
        await task;
    }

    private static TwitchLib.EventSub.Core.EventArgs.Channel.ChannelPointsCustomRewardRedemptionArgs Redemption(
        int cost
    ) =>
        new()
        {
            Payload =
                new TwitchLib.EventSub.Core.Models.EventSubNotificationPayload<TwitchLib.EventSub.Core.SubscriptionTypes.Channel.ChannelPointsCustomRewardRedemption>
                {
                    Event =
                        new TwitchLib.EventSub.Core.SubscriptionTypes.Channel.ChannelPointsCustomRewardRedemption
                        {
                            UserId = "123456789",
                            UserLogin = "pyro",
                            UserName = "pyro",
                            Reward =
                                new TwitchLib.EventSub.Core.Models.ChannelPoints.RedemptionReward
                                {
                                    Cost = cost,
                                    Id = "reward",
                                },
                        },
                },
        };

    private sealed class StubGame(int cost) : ITwitchMiniGame
    {
        public bool IsReuseRewardForAddMechanic { get; set; }

        public bool IsGameRunning { get; set; }

        public string Name => "stub";

        public int Starts { get; private set; }

        public int Cancels { get; private set; }

        public int RedeemCalls { get; private set; }

        public List<string> Messages { get; } = [];

        public int GetGameCost() => cost;

        public Task GameStart(
            string userName,
            string userId,
            CancellationToken cancellationToken = default
        )
        {
            Starts++;

            return Task.CompletedTask;
        }

        public Task CancelAsync()
        {
            Cancels++;

            return Task.CompletedTask;
        }

        public Task OnChatMessage(string userName, string userId, string message)
        {
            Messages.Add(message);

            return Task.CompletedTask;
        }

        public Task<bool> OnRewardRedemption(string userName, string userId, int cost)
        {
            RedeemCalls++;

            return Task.FromResult(true);
        }
    }
}
