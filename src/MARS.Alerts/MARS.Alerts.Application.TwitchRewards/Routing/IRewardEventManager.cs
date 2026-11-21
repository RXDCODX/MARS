using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards.Routing;

public interface IRewardEventManager
{
    public event Func<ChatEventMessageEvent, CancellationToken, Task> OnTwitchMessage;
    public event Func<RewardRedeemedEvent, CancellationToken, Task> OnTwitchRewardRedeem;
    public event Func<EventSubRawEvent, CancellationToken, Task> OnTwitchEvent;
    public event Func<RewardInputChatMessageEvent, CancellationToken, Task> OnTwitchRewardInput;
    internal Task HandleTwitchChatMessageAsync(ChatEventMessageEvent message, CancellationToken ct);

    internal Task HandleTwitchEventAsync(EventSubRawEvent message, CancellationToken ct);

    internal Task HandleTwitchRewardInputAsync(
        RewardInputChatMessageEvent message,
        CancellationToken ct
    );
    internal Task HandleRewardRedeemedAsync(RewardRedeemedEvent message, CancellationToken ct);
}
