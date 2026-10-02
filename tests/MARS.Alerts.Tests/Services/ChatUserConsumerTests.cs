using System.Reflection;
using System.Text.Json;
using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Потребитель сообщений чата.
///
/// Проверяется, что сообщения без автора не попадают в эффекты: MIKU MIKU BEAM
/// набирает участников окна, и сообщение от анонима не должно занимать в нём
/// место.
/// </summary>
public class ChatUserConsumerTests
{
    [Fact]
    public void MessageIsPassedToHandlers()
    {
        var handler = new Mock<IChatUserTrackingHandler>();
        var consumer = Create(handler.Object);

        Handle(consumer, Chat("123456789"));

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
        var consumer = Create(first.Object, second.Object);

        Handle(consumer, Chat("123456789"));

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
        var consumer = Create(handler.Object);

        Handle(consumer, Chat(userId));

        handler.Verify(
            instance => instance.TrackChatUser(It.IsAny<ChatMessageEvent>()),
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
        var consumer = Create(handler.Object);

        Handle(consumer, "null");

        handler.Verify(
            instance => instance.TrackChatUser(It.IsAny<ChatMessageEvent>()),
            Times.Never
        );
    }

    /// <summary>
    /// Битое тело поднимает исключение наружу — так base-класс отправляет
    /// сообщение в ретрай и DLQ. Молча проглоченное сообщение потерялось бы
    /// навсегда.
    /// </summary>
    [Fact]
    public void MalformedBodyFailsForRetry()
    {
        var handler = new Mock<IChatUserTrackingHandler>();
        var consumer = Create(handler.Object);

        Assert.Throws<TargetInvocationException>(() => Handle(consumer, "{ это не json"));
        handler.Verify(
            instance => instance.TrackChatUser(It.IsAny<ChatMessageEvent>()),
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
        var consumer = Create();

        Handle(consumer, Chat("123456789"));
    }

    private static ChatUserConsumer Create(params IChatUserTrackingHandler[] handlers) =>
        new(Options.Create(new RabbitMqOptions()), handlers, NullLogger<ChatUserConsumer>.Instance);

    private static void Handle(ChatUserConsumer consumer, string json)
    {
        var method = typeof(ChatUserConsumer).GetMethod(
            "HandleMessageAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        method.Invoke(consumer, [RabbitMqConfig.MessageReceived, json, CancellationToken.None]);
    }

    private static string Chat(string userId) =>
        JsonSerializer.Serialize(new ChatMessageEvent { UserId = userId, UserName = "Pyro" });
}
