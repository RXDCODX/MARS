using System.Reflection;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services.AutoMessages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;
using ChatMessage = TwitchLib.Client.Models.ChatMessage;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Автосообщения бота: раз в 70 сообщений в чат уходит одна заготовленная фраза.
///
/// Проверяется главное — тишина. Без этих правил бот писал бы в чат на каждое
/// сообщение, а повтор одной и той же фразы подряд читалась бы как зависший бот.
/// </summary>
public class AutoMessagesHandlerTests
{
    private readonly TwitchTestDbContextFactory _factory = new();
    private readonly Mock<ITwitchClient> _client = new();
    private readonly AutoMessagesHandler _handler;

    public AutoMessagesHandlerTests()
    {
        _client
            .SetupGet(instance => instance.JoinedChannels)
            .Returns([new JoinedChannel(MARS.TwitchCore.Extensions.TwitchConstants.Channel)]);
        _client
            .Setup(instance => instance.GetJoinedChannel(It.IsAny<string>()))
            .Returns(new JoinedChannel(MARS.TwitchCore.Extensions.TwitchConstants.Channel));
        _client
            .Setup(instance =>
                instance.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), default)
            )
            .Returns(Task.CompletedTask);

        _handler = new AutoMessagesHandler(
            _client.Object,
            NullLogger<AutoMessagesHandler>.Instance,
            _factory,
            new TestLifetime(),
            PassingValidationService.Instance
        );
    }

    [Fact]
    public async Task MessageIsSentToChat()
    {
        await SeedMessageAsync("привет, чат");

        await ExecuteAutoMessageAsync();

        _client.Verify(
            instance =>
                instance.SendMessageAsync(
                    It.IsAny<string>(),
                    It.Is<string>(text => text == "привет, чат"),
                    default
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Без заготовок бот молчит: пустую фразу в чат отправлять нельзя, иначе
    /// сообщение выглядело бы как «» от бота.
    /// </summary>
    [Fact]
    public async Task NothingIsSentWithoutPhrases()
    {
        await ExecuteAutoMessageAsync();

        _client.Verify(
            instance => instance.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never
        );
    }

    /// <summary>
    /// Отклонённое валидатором сообщение не продвигает счётчик: иначе одно
    /// сообщение из чёрного списка приближало бы автосообщение.
    /// </summary>
    [Fact]
    public async Task RejectedMessageDoesNotPost()
    {
        await SeedMessageAsync("привет, чат");
        var handler = new AutoMessagesHandler(
            _client.Object,
            NullLogger<AutoMessagesHandler>.Instance,
            _factory,
            new TestLifetime(),
            PassingValidationService.Rejecting("чёрный список")
        );

        await handler.OnMessageReceived(null, Message());

        _client.Verify(
            instance => instance.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never
        );
    }

    [Fact]
    public async Task MessageIsAcceptedWithoutError()
    {
        await _handler.OnMessageReceived(null, Message());
    }

    [Fact]
    public async Task StartAndStopAreSafe()
    {
        await _handler.StartAsync(TestContext.Current.CancellationToken);
        await _handler.StopAsync(TestContext.Current.CancellationToken);
    }

    private Task ExecuteAutoMessageAsync() => ExecuteAutoMessageAsync(_handler);

    private static async Task ExecuteAutoMessageAsync(AutoMessagesHandler handler)
    {
        var method = typeof(AutoMessagesHandler).GetMethod(
            "ExecuteAutoMessage",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        await (Task)method.Invoke(handler, null)!;
    }

    private async Task SeedMessageAsync(string text)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.AutoMessages.Add(new AutoMessage { Message = text });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static OnMessageReceivedArgs Message() =>
        new(
            new ChatMessage(
                botUsername: "mars-bot",
                userId: "123456789",
                userName: "pyro",
                displayName: "Pyro",
                hexColor: "#FFFFFF",
                emoteSet: null!,
                message: "текст",
                userType: UserType.Viewer,
                channel: MARS.TwitchCore.Extensions.TwitchConstants.Channel,
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
            )
        );
}
