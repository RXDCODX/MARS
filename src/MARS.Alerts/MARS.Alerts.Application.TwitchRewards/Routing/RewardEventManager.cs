using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards.Routing;

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

    Task IRewardEventManager.HandleRewardRedeemedAsync(
        RewardRedeemedEvent message,
        CancellationToken ct
    )
    {
        ArgumentNullException.ThrowIfNull(message);

        return OnTwitchRewardRedeem.Invoke(message, ct);
    }

internal Task HandleTwitchChatMessageAsync(ChatEventMessageEvent message, CancellationToken ct) =>
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
