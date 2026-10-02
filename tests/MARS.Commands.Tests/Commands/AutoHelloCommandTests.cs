using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.Shared.Clients;
using Moq;

namespace MARS.Commands.Tests.Commands;

public class AutoHelloCommandTests
{
    private static Mock<IWaifuGachaClient> BuildClient(bool enabled)
    {
        var client = new Mock<IWaifuGachaClient>();
        client
            .Setup(c => c.ToggleAutoHelloAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(enabled);

        return client;
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
        var client = BuildClient(true);

        var result = await new AutoHelloCommand(client.Object).ExecuteAsync(
            new Dictionary<string, object> { ["user"] = new StubTwitchUser("77") },
            cancellationToken: ct
        );

        Assert.True(result.Success);
        client.Verify(c => c.ToggleAutoHelloAsync("77", It.IsAny<CancellationToken>()), Times.Once);
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
