using System.Text.Json;
using System.Text.Json.Serialization;
using MARS.Alerts.Application.TwitchMessages;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Ретрансляция сообщений чата Twitch в оверлейный хаб: листенеры должны
/// класть в broadcaster <see cref="TelegramusEvent"/> с веткой
/// <c>NewMessage</c>/<c>DeleteMessage</c>, иначе экраны /chatv и /chath остаются
/// пустыми (сообщение не доходит до браузера).
/// </summary>
public class OverlayChatMessageListenerTests
{
    [Fact]
    public async Task OverlayChatMessageListener_BroadcastsNewMessage()
    {
        var broadcaster = new EventBroadcaster<TelegramusEvent>();
        using var subscription = broadcaster.Subscribe();
        var listener = new OverlayChatMessageListener(
            NullLogger<OverlayChatMessageListener>.Instance,
            broadcaster
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

        Assert.True(subscription.Reader.TryRead(out var notification));
        Assert.Equal(TelegramusEvent.EventOneofCase.NewMessage, notification!.EventCase);
        Assert.Equal("message-1", notification!.NewMessage!.Id);

        // MessageJson должен быть camelCase-JSON сообщения: фронтенд разбирает
        // его через normalizeChatMessage, который читает поля userName, chatColor,
        // isModerator, isVip, message.
        using var document = JsonDocument.Parse(notification!.NewMessage!.MessageJson);
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
        var broadcaster = new EventBroadcaster<TelegramusEvent>();
        using var subscription = broadcaster.Subscribe();
        var listener = new OverlayChatMessageDeletedListener(
            NullLogger<OverlayChatMessageDeletedListener>.Instance,
            broadcaster
        );

        await listener.Handle(
            new ChatMessageDeletedEvent { MessageId = "message-9", Channel = "mars" },
            TestContext.Current.CancellationToken
        );

        Assert.True(subscription.Reader.TryRead(out var notification));
        Assert.Equal(TelegramusEvent.EventOneofCase.DeleteMessage, notification!.EventCase);
        Assert.Equal("message-9", notification!.DeleteMessage!.Id);
    }
}
