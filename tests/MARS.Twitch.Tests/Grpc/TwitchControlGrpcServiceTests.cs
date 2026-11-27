using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MARS.Twitch.Api.Grpc;
using MARS.Twitch.Infrastructure.Services;
using MARS.Twitch.Infrastructure.Services.Connection;
using MARS.Twitch.Infrastructure.Services.EventSub;
using MARS.Twitch.Infrastructure.Services.MiniGamesStats;
using MARS.Twitch.Tests.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Interfaces;

namespace MARS.Twitch.Tests.Grpc;

public class TwitchControlGrpcServiceTests
{
    [Fact]
    public async Task GetConnectionStateReportsEventSubAndChat()
    {
        var eventSub = new Mock<EventSubService>(
            Mock.Of<ITwitchAPI>(),
            NullLogger<EventSubService>.Instance,
            TwitchTokenServiceStub.WithoutToken(),
            Mock.Of<IHostApplicationLifetime>(),
            OfflineEventSub.Create()
        );
        eventSub.SetupGet(service => service.IsWebSocketConnected).Returns(true);
        var connection = new Mock<ITwitchConnectionState>();
        connection.SetupGet(service => service.IsConnected).Returns(false);
        var service = new TwitchControlGrpcService(
            Mock.Of<ITwitchAPI>(),
            Mock.Of<ITwitchClient>(),
            TwitchTokenServiceStub.WithoutToken(),
            Mock.Of<ILeaderboardService>(),
            eventSub.Object,
            connection.Object,
            NullLogger<TwitchControlGrpcService>.Instance
        );

        var response = await service.GetConnectionState(new Empty(), Mock.Of<ServerCallContext>());

        Assert.True(response.IsEventSubConnected);
        Assert.False(response.IsTwitchChatConnected);
    }
}
