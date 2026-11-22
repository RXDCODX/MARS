using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards.Routing;

/// <summary>
/// Шина входящих Twitch-событий обработчиков наград.
/// Все методы публичные — интерфейс проксируется тестами.
/// </summary>
public interface IRewardEventManager
{
    event Func<ChatEventMessageEvent, CancellationToken, Task> OnTwitchMessage;
    event Func<RewardRedeemedEvent, CancellationToken, Task> OnTwitchRewardRedeem;
    event Func<EventSubRawEvent, CancellationToken, Task> OnTwitchEvent;
    event Func<RewardInputChatMessageEvent, CancellationToken, Task> OnTwitchRewardInput;

    Task HandleTwitchChatMessageAsync(ChatEventMessageEvent message, CancellationToken ct);
    Task HandleTwitchEventAsync(EventSubRawEvent message, CancellationToken ct);
    Task HandleTwitchRewardInputAsync(RewardInputChatMessageEvent message, CancellationToken ct);
    Task HandleRewardRedeemedAsync(RewardRedeemedEvent message, CancellationToken ct);
}
