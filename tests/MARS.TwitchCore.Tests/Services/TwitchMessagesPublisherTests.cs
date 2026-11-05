using System.Reflection;
using System.Threading.Tasks;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.Rewards;
using Moq;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;
using Wolverine;
using ChatMessage = TwitchLib.Client.Models.ChatMessage;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Публикатор событий чата: единственное место, откуда сообщения попадают и в
/// брокер, и в оверлей.
///
/// Проверяется разведение потоков. Обычное сообщение уходит обоим адресатам,
/// сообщение через награду — отдельным типом: в оверлей чата такие сообщения
/// не рисуются, как и в монолите, но по ним поднимается привязанный к награде
/// алерт.
/// </summary>
public class TwitchMessagesPublisherTests
{
    private readonly Mock<ITwitchClient> _client = new();
    private readonly Mock<IMessageBus> _bus = new();
    private readonly Mock<ITelegramusNotifier> _notifier = new();
    private readonly List<object> _published = [];
    private readonly TwitchMessagesPublisher _publisher;

    public TwitchMessagesPublisherTests()
    {
        _bus.Setup(instance =>
                instance.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions>())
            )
            .Callback<object, DeliveryOptions>((m, _) => _published.Add(m))
            .Returns(ValueTask.CompletedTask);
        _notifier
            .Setup(instance =>
                instance.NewMessage(It.IsAny<string>(), It.IsAny<ChatMessageReceivedEvent>())
            )
            .Returns(Task.CompletedTask);
        _notifier
            .Setup(instance => instance.DeleteMessage(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _publisher = new TwitchMessagesPublisher(
            _client.Object,
            new TestLifetime(),
            PassingValidationService.Instance,
            _bus.Object,
            _notifier.Object
        );
    }

    [Fact]
    public async Task MessageGoesToBrokerAndToOverlay()
    {
        await InvokeAsync("ClientOnOnMessageReceived", Message());

        Assert.Single(_published);
        var chatMessage = Assert.IsType<ChatMessageReceivedEvent>(Assert.Single(_published));
        Assert.Equal("123456789", chatMessage.UserId);
        _notifier.Verify(
            instance => instance.NewMessage("message-1", It.IsAny<ChatMessageReceivedEvent>()),
            Times.Once
        );
    }

    [Fact]
    public async Task MessageCarriesChatDetails()
    {
        await InvokeAsync("ClientOnOnMessageReceived", Message());

        var message = Assert.IsType<ChatMessageReceivedEvent>(Assert.Single(_published));
        Assert.Equal("123456789", message.UserId);
        Assert.Equal("текст", message.Message);
        Assert.Equal("#FFFFFF", message.ChatColor);
        Assert.False(message.IsBroadcaster);
        Assert.True(message.IsModerator);
    }

    /// <summary>
    /// Сообщение через награду уходит отдельным типом и не попадает в оверлей:
    /// потребители обычного чата не должны на него подписываться.
    /// </summary>
    [Fact]
    public async Task RewardMessageGoesToItsOwnTypeOnly()
    {
        await InvokeAsync("ClientOnOnMessageReceived", Message(rewardId: "reward-1"));

        Assert.Single(_published);
        Assert.IsType<RewardInputMessageEvent>(Assert.Single(_published));
        _notifier.Verify(
            instance =>
                instance.NewMessage(It.IsAny<string>(), It.IsAny<ChatMessageReceivedEvent>()),
            Times.Never
        );
    }

    [Fact]
    public async Task BroadcasterIsRecognized()
    {
        await InvokeAsync("ClientOnOnMessageReceived", Message(userId: TwitchConstants.ChannelId));

        var message = Assert.IsType<ChatMessageReceivedEvent>(Assert.Single(_published));
        Assert.True(message.IsBroadcaster);
    }

    [Fact]
    public async Task ClearedMessageIsDeletedEverywhere()
    {
        await InvokeAsync(
            "ClientOnOnMessageCleared",
            new OnMessageClearedArgs(TwitchConstants.Channel, string.Empty, "message-9", default)
        );

        var deleted = Assert.IsType<ChatMessageDeletedEvent>(Assert.Single(_published));
        Assert.Equal("message-9", deleted.MessageId);
        _notifier.Verify(instance => instance.DeleteMessage("message-9"), Times.Once);
    }

    /// <summary>
    /// Очистка сообщения в чужом канале игнорируется: в этом сервисе один канал,
    /// и подчищать чужие сообщения у него нечем.
    /// </summary>
    [Fact]
    public async Task ClearedMessageOfForeignChannelIsIgnored()
    {
        await InvokeAsync(
            "ClientOnOnMessageCleared",
            new OnMessageClearedArgs("other", string.Empty, "message-9", default)
        );

        Assert.Empty(_published);
        _notifier.Verify(instance => instance.DeleteMessage(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Отклонённое валидатором сообщение не публикуется: в чёрный список попадают
    /// и обычные зрители, и их сообщения не должны нигде всплывать.
    /// </summary>
    [Fact]
    public async Task RejectedMessageIsNotPublished()
    {
        var publisher = new TwitchMessagesPublisher(
            _client.Object,
            new TestLifetime(),
            PassingValidationService.Rejecting("пользователь в чёрном списке"),
            _bus.Object,
            _notifier.Object
        );

        await InvokeAsync(publisher, "ClientOnOnMessageReceived", Message());

        Assert.Empty(_published);
    }

    [Fact]
    public async Task StartSubscribesToClient()
    {
        await _publisher.StartAsync(TestContext.Current.CancellationToken);
        await _publisher.StopAsync(TestContext.Current.CancellationToken);
    }

    private Task InvokeAsync(string handler, object args) => InvokeAsync(_publisher, handler, args);

    private static async Task InvokeAsync(
        TwitchMessagesPublisher publisher,
        string handler,
        object args
    )
    {
        var method = typeof(TwitchMessagesPublisher).GetMethod(
            handler,
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        await (Task)method.Invoke(publisher, [null, args])!;
    }

    private static OnMessageReceivedArgs Message(
        string? rewardId = null,
        string userId = "123456789"
    ) => new(ChatMessage(rewardId, userId));

    /// <summary>
    /// CustomRewardId у ChatMessage — поле только для чтения: в TwitchLib оно
    /// заполняется разбором IRC-тега, поэтому из теста ставится рефлексией по
    /// backing-полю, а не через сеттер, которого нет.
    /// </summary>
    private static ChatMessage ChatMessage(string? rewardId, string userId)
    {
        var message = new ChatMessage(
            botUsername: "mars-bot",
            userId: userId,
            userName: "login",
            displayName: "Pyro",
            hexColor: "#FFFFFF",
            emoteSet: null!,
            message: "текст",
            userType: UserType.Moderator,
            channel: TwitchConstants.Channel,
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
            userDetail: new UserDetail(UserDetails.Moderator)
        );

        typeof(ChatMessage)
            .GetField(
                "<CustomRewardId>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic
            )!
            .SetValue(message, rewardId);

        return message;
    }
}
