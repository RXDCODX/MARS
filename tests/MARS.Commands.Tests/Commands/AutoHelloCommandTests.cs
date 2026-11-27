using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Grpc.WaifuGacha;
using Moq;

namespace MARS.Commands.Tests.Commands;

public class AutoHelloCommandTests
{
    private static Mock<IMarsGrpcClient> BuildClient(bool enabled)
    {
        var grpc = new Mock<IMarsGrpcClient>();
        var waifu = new Mock<IWaifuGachaApi>();
        waifu
            .Setup(c => c.ToggleAutoHelloAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(enabled);
        grpc.SetupGet(client => client.WaifuGacha).Returns(waifu.Object);

        return grpc;
    }

    [Theory]
    [InlineData(true, "включено")]
    [InlineData(false, "выключено")]
    public async Task Toggle_ReportsTheNewState(bool enabled, string expected)
    {
        var ct = TestContext.Current.CancellationToken;
        var command = new AutoHelloCommand(BuildClient(enabled).Object);

        var result = await command.ExecuteAsync(
            new Dictionary<string, object> { ["userId"] = "5" },
            cancellationToken: ct
        );

        Assert.True(result.Success);
        Assert.Contains(expected, result.Text);
    }

    [Fact]
    public async Task Toggle_UsesTheCallerIdFromTheTwitchAdapter()
    {
        var ct = TestContext.Current.CancellationToken;
        var waifu = new Mock<IWaifuGachaApi>();
        waifu
            .Setup(c => c.ToggleAutoHelloAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var grpc = new Mock<IMarsGrpcClient>();
        grpc.SetupGet(client => client.WaifuGacha).Returns(waifu.Object);

        var result = await new AutoHelloCommand(grpc.Object).ExecuteAsync(
            new Dictionary<string, object> { ["user"] = new StubTwitchUser("77") },
            cancellationToken: ct
        );

        Assert.True(result.Success);
        waifu.Verify(c => c.ToggleAutoHelloAsync("77", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Toggle_Fails_WhenCallerIsUnknown()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = new AutoHelloCommand(BuildClient(true).Object);

        var result = await command.ExecuteAsync(
            new Dictionary<string, object>(),
            cancellationToken: ct
        );

        Assert.False(result.Success);
        Assert.Equal(CommandErrorCode.BadArguments, result.ErrorCode);
    }

    private sealed record StubTwitchUser(string TwitchId);
}
