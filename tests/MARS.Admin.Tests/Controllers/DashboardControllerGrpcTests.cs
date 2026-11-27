using System.Reflection;
using MARS.Admin.Api.Contracts;
using MARS.Admin.Api.Controllers;
using MARS.Admin.Application.Services.Logging;
using MARS.Admin.Application.Services.ServiceManager;
using MARS.Admin.Infrastructure.Postgres.Data;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Grpc.Twitch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Admin.Tests.Controllers;

public class DashboardControllerGrpcTests
{
    [Fact]
    public async Task PopulateConnectionsUsesTwitchGrpcState()
    {
        var twitch = new Mock<ITwitchApi>();
        twitch
            .Setup(api => api.GetConnectionStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TwitchConnectionState(true, false));
        var grpc = new Mock<IMarsGrpcClient>();
        grpc.SetupGet(client => client.Twitch).Returns(twitch.Object);
        var controller = CreateController(grpc.Object);
        var response = new DashboardOverviewResponse();

        await PopulateConnectionsAsync(controller, response);

        Assert.True(response.Connections["eventSub"]);
        Assert.False(response.Connections["twitchChat"]);
        twitch.Verify(
            api => api.GetConnectionStateAsync(TestContext.Current.CancellationToken),
            Times.Once
        );
    }

    private static DashboardController CreateController(IMarsGrpcClient grpcClient) =>
        new(
            Mock.Of<IServiceManager>(),
            Mock.Of<ILogQueryService>(),
            Mock.Of<IHttpClientFactory>(),
            Mock.Of<IDbContextFactory<AdminDbContext>>(),
            new ConfigurationBuilder().Build(),
            grpcClient,
            NullLogger<DashboardController>.Instance
        );

    private static async Task PopulateConnectionsAsync(
        DashboardController controller,
        DashboardOverviewResponse response
    )
    {
        var method = typeof(DashboardController).GetMethod(
            "PopulateConnectionsAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Assert.NotNull(method);

        var task = Assert.IsAssignableFrom<Task>(
            method.Invoke(controller, [response, TestContext.Current.CancellationToken])
        );
        await task;
    }
}
