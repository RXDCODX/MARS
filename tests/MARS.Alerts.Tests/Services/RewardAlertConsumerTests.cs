using MARS.Alerts.Application.TwitchRewards;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Listeners;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Шина событий наград: все подписанные обработчики получают редемпшен и
/// фильтруют его сами (валидацией стоимости/владельца канала).
/// </summary>
public class RewardAlertConsumerTests
{
    [Fact]
    public async Task RewardEventIsFannedOutToAllSubscribers()
    {
        var first = new StubHandler();
        var second = new StubHandler();
        var listener = Create(first, second);

        await listener.Handle(
            new RewardRedeemedEvent { UserId = "42", UserName = "viewer" },
            TestContext.Current.CancellationToken
        );

        Assert.Equal(1, first.Calls);
        Assert.Equal(1, second.Calls);
    }

    [Fact]
    public void RewardCodeIsNormalizedTitle()
    {
        Assert.Equal("аяка", RewardRouting.KeyFor("аяка"));
        Assert.Equal("phonkedit", RewardRouting.KeyFor("Phonk Edit"));
        Assert.Equal(string.Empty, RewardRouting.KeyFor("  "));
    }

    private static RewardAlertListener Create(params IRewardAlertHandler[] handlers) =>
        new(new RewardEventManager(handlers, NullLogger<RewardEventManager>.Instance));

    private sealed class StubHandler : RewardHandlerBase
    {
        public int Calls { get; private set; }

        protected override Task OnRewardRedeemedAsync(
            RewardRedeemedEvent rewardEvent,
            CancellationToken ct
        )
        {
            Calls++;
            return Task.CompletedTask;
        }
    }
}