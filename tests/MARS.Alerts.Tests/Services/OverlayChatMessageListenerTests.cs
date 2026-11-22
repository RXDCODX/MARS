using System.Text.Json;
using MARS.Alerts.Application.TwitchMessages;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Ретрансляция сообщений чата Twitch в оверлейный хаб.
/// </summary>
public class OverlayChatMessageListenerTests
{
    [Fact]
    public async Task OverlayChatMessageListener_BroadcastsNewMessage()
    {
        var overlay = new TestOverlayHub();
        var listener = new OverlayChatMessageListener(
            NullLogger<OverlayChatMessageListener>.Instance,
            overlay.Context
        );

        await listener.Handle(
            new ChatEventMessageEvent
            {
                Id = "message-1",
                UserId = "123456789",
                UserName = "viewer",
                Message = "текст",
                ChatColor = "#FFFFFF",
                IsModerator = true,
                IsVip = false,
                IsBroadcaster = false,
            },
            TestContext.Current.CancellationToken
        );

        overlay.ClientsMock.Verify(
            c => c.NewMessage(It.Is<NewMessageEvent>(e => e.Id == "message-1")),
            Times.Once
        );

        NewMessageEvent? sent = null;
        overlay
            .ClientsMock.Invocations.Where(i =>
                i.Method.Name
                == nameof(MARS.Alerts.Models.Overlay.IAlertsHub.NewMessage)
            )
            .ToList()
            .ForEach(i => sent = i.Arguments[0] as NewMessageEvent);

        Assert.NotNull(sent);
        using var document = JsonDocument.Parse(sent!.MessageJson);
        var root = document.RootElement;
        Assert.Equal("viewer", root.GetProperty("userName").GetString());
        Assert.Equal("#FFFFFF", root.GetProperty("chatColor").GetString());
        Assert.True(root.GetProperty("isModerator").GetBoolean());
        Assert.False(root.GetProperty("isVip").GetBoolean());
        Assert.Equal("текст", root.GetProperty("message").GetString());
    }

    [Fact]
    public async Task OverlayChatMessageDeletedListener_BroadcastsDeleteMessage()
    {
        var overlay = new TestOverlayHub();
        var listener = new OverlayChatMessageDeletedListener(
            NullLogger<OverlayChatMessageDeletedListener>.Instance,
            overlay.Context
        );

        await listener.Handle(
            new ChatMessageDeletedEvent { MessageId = "message-9", Channel = "mars" },
            TestContext.Current.CancellationToken
        );

        overlay.ClientsMock.Verify(
            c => c.DeleteMessage(It.Is<DeleteMessageEvent>(e => e.Id == "message-9")),
            Times.Once
        );
    }
}