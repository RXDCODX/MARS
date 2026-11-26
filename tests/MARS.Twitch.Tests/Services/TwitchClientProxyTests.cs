using System.Threading.Tasks;
using MARS.Twitch.Infrastructure.Services.Connection;
using Moq;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;
using Xunit;

namespace MARS.Twitch.Tests.Services;

public class TwitchClientProxyTests
{
    [Fact]
    public void IsConnected_DelegatesToCurrentClient()
    {
        var first = new Mock<ITwitchClient>();
        first.SetupGet(client => client.IsConnected).Returns(true);

        var proxy = TwitchClientProxy.Create();
        TwitchClientProxy.GetProxyInstance(proxy).ReplaceClient(first.Object);

        Assert.True(proxy.IsConnected);

        var second = new Mock<ITwitchClient>();
        second.SetupGet(client => client.IsConnected).Returns(false);
        TwitchClientProxy.GetProxyInstance(proxy).ReplaceClient(second.Object);

        Assert.False(proxy.IsConnected);
    }

    [Fact]
    public async Task JoinChannelAsync_DelegatesToCurrentClient()
    {
        var first = new Mock<ITwitchClient>();
        first
            .Setup(client => client.JoinChannelAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        var proxy = TwitchClientProxy.Create();
        TwitchClientProxy.GetProxyInstance(proxy).ReplaceClient(first.Object);

        await proxy.JoinChannelAsync("first-channel", false);

        first.Verify(client => client.JoinChannelAsync("first-channel", false), Times.Once);

        var second = new Mock<ITwitchClient>();
        second
            .Setup(client => client.JoinChannelAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        TwitchClientProxy.GetProxyInstance(proxy).ReplaceClient(second.Object);

        await proxy.JoinChannelAsync("second-channel", false);

        second.Verify(client => client.JoinChannelAsync("second-channel", false), Times.Once);
        first.Verify(
            client => client.JoinChannelAsync(It.IsAny<string>(), It.IsAny<bool>()),
            Times.Once
        );
    }

    [Fact]
    public async Task OnMessageReceived_SurvivesClientReplacement()
    {
        var first = new Mock<ITwitchClient>();
        var second = new Mock<ITwitchClient>();

        var proxy = TwitchClientProxy.Create();
        TwitchClientProxy.GetProxyInstance(proxy).ReplaceClient(first.Object);

        var received = 0;
        proxy.OnMessageReceived += (_, _) =>
        {
            received++;
            return Task.CompletedTask;
        };

        var message = new ChatMessage(
            botUsername: "mars-bot",
            userId: "123",
            userName: "pyro",
            displayName: "Pyro",
            hexColor: "#FFFFFF",
            emoteSet: null!,
            message: "hello",
            userType: UserType.Viewer,
            channel: "tekken-streamer",
            id: "message-1",
            subscribedMonthCount: 0,
            roomId: "room",
            isMe: false,
            isBroadcaster: false,
            noisy: default,
            rawIrcMessage: string.Empty,
            emoteReplacedMessage: string.Empty,
            badges: [],
            cheerBadge: null!,
            bits: 0,
            bitsInDollars: 0,
            userDetail: default
        );
        var args = new OnMessageReceivedArgs(message);

        first.Raise(client => client.OnMessageReceived += null, first.Object, args);
        Assert.Equal(1, received);

        TwitchClientProxy.GetProxyInstance(proxy).ReplaceClient(second.Object);

        second.Raise(client => client.OnMessageReceived += null, second.Object, args);
        Assert.Equal(2, received);

        first.Raise(client => client.OnMessageReceived += null, first.Object, args);
        Assert.Equal(2, received);
    }
}
