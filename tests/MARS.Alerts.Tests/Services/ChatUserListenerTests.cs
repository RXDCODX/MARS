using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Shared.Messaging;
using Moq;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Слушатель сообщений чата.
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

        listener.Handle(Chat("123456789"), TestContext.Current.CancellationToken);

        handler.Verify(
            instance => instance.TrackChatUser(It.IsAny<ChatMessageEvent>()),
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

        listener.Handle(Chat("123456789"), TestContext.Current.CancellationToken);

        first.Verify(instance => instance.TrackChatUser(It.IsAny<ChatMessageEvent>()), Times.Once);
        second.Verify(instance => instance.TrackChatUser(It.IsAny<ChatMessageEvent>()), Times.Once);
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

        listener.Handle(Chat(userId), TestContext.Current.CancellationToken);

        handler.Verify(
            instance => instance.TrackChatUser(It.IsAny<ChatMessageEvent>()),
            Times.Never
        );
    }

    /// <summary>
    /// Без обработчиков сообщение просто игнорируется — слушатель не должен
    /// требовать наличия эффектов, чтобы работать.
    /// </summary>
    [Fact]
    public void NoHandlersIsFine()
    {
        var listener = Create();

        listener.Handle(Chat("123456789"), TestContext.Current.CancellationToken);
    }

    private static ChatUserListener Create(params IChatUserTrackingHandler[] handlers) =>
        new(handlers);

    private static ChatMessageReceivedEvent Chat(string userId) =>
        new() { UserId = userId, UserName = "Pyro" };
}
