using System.Text.Json;
using MARS.Alerts.Application.Twitch.Rewards;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Потребитель сообщений чата.
///
/// Проверяется, что сообщения без автора не попадают в эффекты: MIKU MIKU BEAM
/// набирает участников окна, и сообщение от анонима не должно занимать в нём
/// место.
/// </summary>
public class ChatUserListenerTests
{
    [Fact]
    public void MessageIsPassedToHandlers()
    {
        var handler = new Mock<IChatUserTrackingHandler>();
        var listener = Create(handler.Object);

        Handle(listener, Chat("123456789"));

        handler.Verify(
            instance => instance.TrackChatUser(It.IsAny<ChatEventMessageEvent>()),
            Times.Once
        );
    }

    /// <summary>
    /// Все зарегистрированные обработчики видят сообщение: эффектов может быть
    /// несколько, и они должны получать поток одинаково.
    /// </summary>
    [Fact]
    public void AllHandlersSeeMessage()
    {
        var first = new Mock<IChatUserTrackingHandler>();
        var second = new Mock<IChatUserTrackingHandler>();
        var listener = Create(first.Object, second.Object);

        Handle(listener, Chat("123456789"));

        first.Verify(
            instance => instance.TrackChatUser(It.IsAny<ChatEventMessageEvent>()),
            Times.Once
        );
        second.Verify(
            instance => instance.TrackChatUser(It.IsAny<ChatEventMessageEvent>()),
            Times.Once
        );
    }

    /// <summary>
    /// Без автора сообщение отбрасывается: аноним не наблюдатель стрима.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MessageWithoutAuthorIsSkipped(string userId)
    {
        var handler = new Mock<IChatUserTrackingHandler>();
        var listener = Create(handler.Object);

        Handle(listener, Chat(userId));

        handler.Verify(
            instance => instance.TrackChatUser(It.IsAny<ChatEventMessageEvent>()),
            Times.Never
        );
    }

    /// <summary>
    /// Тело <c>null</c> — события нет: сообщение пропускается, разбор не падает.
    /// </summary>
    [Fact]
    public void EmptyBodyIsSkipped()
    {
        var handler = new Mock<IChatUserTrackingHandler>();
        var listener = Create(handler.Object);

        Handle(listener, "null");

        handler.Verify(
            instance => instance.TrackChatUser(It.IsAny<ChatEventMessageEvent>()),
            Times.Never
        );
    }

    /// <summary>
    /// Без обработчиков сообщение просто игнорируется — потребитель не должен
    /// требовать наличия эффектов, чтобы работать.
    /// </summary>
    [Fact]
    public void NoHandlersIsFine()
    {
        var listener = Create();

        Handle(listener, Chat("123456789"));
    }

    private static ChatUserListener Create(params IChatUserTrackingHandler[] handlers) =>
        new(handlers, NullLogger<ChatUserListener>.Instance);

    private static void Handle(ChatUserListener listener, string json)
    {
        var message = JsonSerializer.Deserialize<ChatEventMessageEvent>(
            json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        listener.Handle(message!, CancellationToken.None);
    }

    private static string Chat(string userId) =>
        JsonSerializer.Serialize(new ChatEventMessageEvent { UserId = userId, UserName = "Pyro" });
}
