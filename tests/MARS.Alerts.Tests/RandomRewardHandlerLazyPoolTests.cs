using MARS.Alerts.Application.TwitchRewards._1_RandomReward;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Services.Validation;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Grpc.Twitch;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using TwitchChatSendRequest = MARS.Shared.Contracts.Twitch.Control.TwitchChatSendRequest;

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
            Mock.Of<IMarsGrpcClient>(),
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

        var twitch = new Mock<ITwitchApi>();
        twitch
            .Setup(api =>
                api.SendChatMessageAsync(
                    It.IsAny<TwitchChatSendRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);
        var grpc = new Mock<IMarsGrpcClient>();
        grpc.SetupGet(client => client.Twitch).Returns(twitch.Object);

        var handler = new RandomRewardHandler(
            NullLogger<RandomRewardHandler>.Instance,
            serviceProvider.Object,
            grpc.Object,
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
