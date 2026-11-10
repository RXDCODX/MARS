using System.Reflection;
using MARS.Platform.Infrastructure.Grpc.Notifications;
using MARS.Shared.Contracts.Messaging;
using MARS.Twitch.Api.Extensions;
using MARS.Twitch.Api.Services.Rewards;
using MARS.Twitch.Api.Services.Validation;
using Microsoft.Extensions.Hosting;
using Moq;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;
using Wolverine;
using ChatMessage = TwitchLib.Client.Models.ChatMessage;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Публикатор событий чата: единственное место, откуда сообщения попадают в
/// шину Wolverine и в оверлей.
///
/// Проверяется разведение потоков. Обычное сообщение уходит как
/// <see cref="ChatEventMessageEvent">ChatEventMessageEvent</see>, сообщение через
/// награду — как <see cref="RewardInputChatMessageEvent">RewardInputChatMessageEvent</see>,
/// удалённое сообщение — как <see cref="ChatMessageDeletedEvent">ChatMessageDeletedEvent</see>.
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
            .Returns<object, DeliveryOptions?>(
                (message, _) =>
                {
                    _published.Add(message);
                    return ValueTask.CompletedTask;
                }
            );
        _notifier
            .Setup(instance => instance.NewMessage(It.IsAny<string>(), It.IsAny<object>()))
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
    public async Task MessageGoesToBusAndOverlay()
    {
        await InvokeAsync("ClientOnOnMessageReceived", Message());

        var message = Assert.IsType<ChatEventMessageEvent>(Assert.Single(_published));
        Assert.Equal("123456789", message.UserId);
        _notifier.Verify(
            instance => instance.NewMessage("message-1", It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task MessageCarriesChatDetails()
    {
        await InvokeAsync("ClientOnOnMessageReceived", Message());

        var message = Assert.IsType<ChatEventMessageEvent>(Assert.Single(_published));
        Assert.Equal("123456789", message.UserId);
        Assert.Equal("текст", message.Message);
        Assert.Equal("#FFFFFF", message.ChatColor);
        Assert.False(message.IsBroadcaster);
        Assert.True(message.IsModerator);
        Assert.Equal("Pyro", message.DisplayName);
        Assert.Equal(TwitchConstants.Channel, message.Channel);
        Assert.Equal(0, message.Bits);
    }

    /// <summary>
    /// Сообщение через награду уходит как отдельный контракт: потребители обычного
    /// чата не должны на него подписываться, и в оверлей чата оно не рисуется.
    /// </summary>
    [Fact]
    public async Task RewardMessageGoesAsRewardInputEvent()
    {
        await InvokeAsync("ClientOnOnMessageReceived", Message(rewardId: "reward-1"));

        var message = Assert.IsType<RewardInputChatMessageEvent>(Assert.Single(_published));
        Assert.Equal("reward-1", message.Message.CustomRewardId);
        _notifier.Verify(
            instance => instance.NewMessage(It.IsAny<string>(), It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task BroadcasterIsRecognized()
    {
        await InvokeAsync("ClientOnOnMessageReceived", Message(userId: TwitchConstants.ChannelId));

        Assert.True(((ChatEventMessageEvent)Assert.Single(_published)).IsBroadcaster);
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
    public async Task ExecuteAsyncSubscribesOnApplicationStartAndUnsubscribesOnStopping()
    {
        using var lifetime = new ControllableHostApplicationLifetime();
        var publisher = new TestableTwitchMessagesPublisher(
            _client.Object,
            lifetime,
            PassingValidationService.Instance,
            _bus.Object,
            _notifier.Object
        );

        await publisher.ExecuteForTestAsync(TestContext.Current.CancellationToken);

        lifetime.NotifyStarted();
        _client.Raise(client => client.OnMessageReceived += null, Message());

        Assert.Single(_published);

        lifetime.StopApplication();
        _published.Clear();

        _client.Raise(client => client.OnMessageReceived += null, Message());

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

    private sealed class TestableTwitchMessagesPublisher(
        ITwitchClient client,
        IHostApplicationLifetime lifetime,
        ITwitchEventValidationService validator,
        IMessageBus eventBus,
        ITelegramusNotifier notifier
    ) : TwitchMessagesPublisher(client, lifetime, validator, eventBus, notifier)
    {
        public Task ExecuteForTestAsync(CancellationToken cancellationToken) =>
            ExecuteAsync(cancellationToken);
    }

    private sealed class ControllableHostApplicationLifetime : IHostApplicationLifetime, IDisposable
    {
        private readonly CancellationTokenSource _started = new();
        private readonly CancellationTokenSource _stopping = new();
        private readonly CancellationTokenSource _stopped = new();

        public CancellationToken ApplicationStarted => _started.Token;
        public CancellationToken ApplicationStopping => _stopping.Token;
        public CancellationToken ApplicationStopped => _stopped.Token;

        public void NotifyStarted() => _started.Cancel();

        public void StopApplication() => _stopping.Cancel();

        public void Dispose()
        {
            _started.Dispose();
            _stopping.Dispose();
            _stopped.Dispose();
        }
    }
}
