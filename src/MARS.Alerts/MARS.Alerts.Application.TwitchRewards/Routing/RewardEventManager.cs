using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards.Routing;

public sealed class RewardEventManager : IRewardEventManager
{
    /// <summary>
    /// Тестовый конструктор: сразу подключает обработчики к шине событий без
    /// host lifetime (в бою подписка идёт через <c>SubscribeOnStart</c>).
    /// </summary>
    public RewardEventManager(
        IEnumerable<IRewardAlertHandler> handlers,
        ILogger<RewardEventManager>? logger = null
    )
    {
        foreach (var handler in handlers.OfType<RewardHandlerBase>())
        {
            OnTwitchRewardRedeem += handler.HandleAsync;
            OnTwitchMessage += handler.DispatchChatMessageAsync;
            OnTwitchRewardInput += (e, ct) => handler.DispatchRewardInputAsync(e.Message, ct);
            OnTwitchEvent += handler.DispatchEventSubAsync;
        }
    }

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