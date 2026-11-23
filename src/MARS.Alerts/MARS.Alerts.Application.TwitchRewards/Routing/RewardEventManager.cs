using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards.Routing;

/// <summary>
/// Диспетчер входящих Twitch-событий обработчиков наград. Не знает про
/// конкретные обработчики: каждый из них подписывается сам (через DI и
/// <see cref="RewardHandlerBase.SubscribeOnStart"/>) на его события, а менеджер
/// их вызывает.
/// </summary>
public sealed class RewardEventManager : IRewardEventManager
{
    public event Func<ChatEventMessageEvent, CancellationToken, Task> OnTwitchMessage = (_, _) =>
        Task.CompletedTask;
    public event Func<RewardRedeemedEvent, CancellationToken, Task> OnTwitchRewardRedeem = (_, _) =>
        Task.CompletedTask;
    public event Func<EventSubRawEvent, CancellationToken, Task> OnTwitchEvent = (_, _) =>
        Task.CompletedTask;
    public event Func<RewardInputChatMessageEvent, CancellationToken, Task> OnTwitchRewardInput = (
        _,
        _
    ) => Task.CompletedTask;

    public Task HandleRewardRedeemedAsync(RewardRedeemedEvent message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        return OnTwitchRewardRedeem.Invoke(message, ct);
    }

    public Task HandleTwitchChatMessageAsync(ChatEventMessageEvent message, CancellationToken ct) =>
        OnTwitchMessage.Invoke(message, ct);

    public Task HandleTwitchRewardInputAsync(
        RewardInputChatMessageEvent message,
        CancellationToken ct
    ) => OnTwitchRewardInput.Invoke(message, ct);

    public Task HandleTwitchEventAsync(EventSubRawEvent message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        return OnTwitchEvent.Invoke(message, ct);
    }
}
