using MARS.Alerts.Application.Twitch.Rewards;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Alerts.Tests.Services;

/// <summary>Проверка маршрутизации обычных сообщений чата через базовые hooks наград.</summary>
public class ChatUserListenerTests
{
    [Fact]
    public async Task MessageIsPassedToHandlers()
    {
        var handler = new ChatProbeHandler();
        var listener = Create(handler);
        var message = Chat("123456789");

        await listener.Handle(message, CancellationToken.None);

        Assert.Same(message, Assert.Single(handler.Messages));
    }

    [Fact]
    public async Task AllHandlersSeeMessage()
    {
        var first = new ChatProbeHandler();
        var second = new ChatProbeHandler();
        var listener = Create(first, second);
        var message = Chat("123456789");

        await listener.Handle(message, CancellationToken.None);

        Assert.Same(message, Assert.Single(first.Messages));
        Assert.Same(message, Assert.Single(second.Messages));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MessageWithoutAuthorIsSkipped(string userId)
    {
        var handler = new ChatProbeHandler();
        var listener = Create(handler);

        await listener.Handle(Chat(userId), CancellationToken.None);

        Assert.Empty(handler.Messages);
    }

    [Fact]
    public async Task EmptyBodyIsSkipped()
    {
        var handler = new ChatProbeHandler();
        var listener = Create(handler);

        await listener.Handle(null!, CancellationToken.None);

        Assert.Empty(handler.Messages);
    }

    [Fact]
    public async Task NoHandlersIsFine()
    {
        var listener = Create();

        await listener.Handle(Chat("123456789"), CancellationToken.None);
    }

    private static ChatUserListener Create(params RewardHandlerBase[] handlers)
    {
        var manager = new RewardEventManager(handlers, NullLogger<RewardEventManager>.Instance);

        return new ChatUserListener(manager, NullLogger<ChatUserListener>.Instance);
    }

    private static ChatEventMessageEvent Chat(string userId) =>
        new() { UserId = userId, UserName = "Pyro" };

    private sealed class ChatProbeHandler : RewardHandlerBase
    {
        public override string RewardKey => "chat-probe";

        public List<ChatEventMessageEvent> Messages { get; } = [];

        protected override Task OnRewardRedeemedAsync(
            RewardRedeemedEvent rewardEvent,
            CancellationToken ct
        ) => Task.CompletedTask;

        protected override Task OnTwitchChatMessageAsync(
            ChatEventMessageEvent message,
            CancellationToken ct
        )
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
