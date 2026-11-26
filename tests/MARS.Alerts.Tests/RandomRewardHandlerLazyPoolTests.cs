using MARS.Alerts.Application.TwitchRewards._1_RandomReward;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Services.Validation;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Twitch.Rest;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MARS.Alerts.Tests;

public class RandomRewardHandlerLazyPoolTests
{
    [Fact]
    public void Constructor_DoesNotResolveHandlers()
    {
        var serviceProvider = new Mock<IServiceProvider>();

        _ = new RandomRewardHandler(
            NullLogger<RandomRewardHandler>.Instance,
            serviceProvider.Object,
            Mock.Of<ITwitchInternalClient>(),
            Mock.Of<ITwitchEventValidationService>(),
            Mock.Of<IHostApplicationLifetime>(),
            Mock.Of<IRewardEventManager>()
        );

        serviceProvider.Verify(sp => sp.GetService(It.IsAny<Type>()), Times.Never);
    }

    [Fact]
    public async Task Redeem_ResolvesHandlersFromProvider()
    {
        var handlers = new List<IRewardAlertHandler>();
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(sp => sp.GetService(typeof(IEnumerable<IRewardAlertHandler>)))
            .Returns(handlers);

        var handler = new RandomRewardHandler(
            NullLogger<RandomRewardHandler>.Instance,
            serviceProvider.Object,
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

        serviceProvider.Verify(
            sp => sp.GetService(typeof(IEnumerable<IRewardAlertHandler>)),
            Times.AtLeastOnce
        );
    }
}
