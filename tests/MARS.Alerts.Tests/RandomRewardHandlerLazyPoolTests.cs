using MARS.Alerts.Application.TwitchRewards._1_RandomReward;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Twitch.Rest;
using MARS.TestKit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MARS.Alerts.Tests;

public class RandomRewardHandlerLazyPoolTests
{
    [Fact]
    public void Constructor_DoesNotInvokePool()
    {
        var poolCalled = false;
        Func<IEnumerable<IRewardAlertHandler>> provider = () =>
        {
            poolCalled = true;
            return [];
        };

        _ = new RandomRewardHandler(
            NullLogger<RandomRewardHandler>.Instance,
            provider,
            Mock.Of<ITwitchInternalClient>(),
            Mock.Of<ITwitchEventValidationService>(),
            Mock.Of<IHostApplicationLifetime>(),
            Mock.Of<IRewardEventManager>()
        );

        Assert.False(poolCalled);
    }

    [Fact]
    public async Task Redeem_InvokesPool()
    {
        var poolCalled = false;
        Func<IEnumerable<IRewardAlertHandler>> provider = () =>
        {
            poolCalled = true;
            return [];
        };

        var handler = new RandomRewardHandler(
            NullLogger<RandomRewardHandler>.Instance,
            provider,
            Mock.Of<ITwitchInternalClient>(),
            Mock.Of<ITwitchEventValidationService>(),
            Mock.Of<IHostApplicationLifetime>(),
            Mock.Of<IRewardEventManager>()
        );

        var evt = new RewardRedeemedEvent
        {
            Cost = 1,
            UserId = "u",
            UserName = "u",
        };

        await handler.HandleAsync(evt, CancellationToken.None);

        Assert.True(poolCalled);
    }
}
