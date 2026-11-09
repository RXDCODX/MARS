using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Общая основа Twitch-наград. Скрывает общую точку входа для редемпшена и
/// предоставляет необязательные hooks для обычных сообщений чата и reward-input.
/// </summary>
/// <remarks>
/// Конкретная награда реализует только <see cref="OnRewardRedeemedAsync"/> и
/// переопределяет обработку сообщений, которые ей действительно нужны.
/// <see cref="RewardEventManager"/> вызывает hooks для входящих событий Twitch.
/// </remarks>
public abstract class RewardHandlerBase : IRewardAlertHandler
{
    public abstract string RewardKey { get; }

    /// <summary>Единая точка входа при погашении конкретной награды.</summary>
    public Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(rewardEvent);
        return OnRewardRedeemedAsync(rewardEvent, ct);
    }

    /// <summary>Логика погашения, специфичная для конкретной награды.</summary>
    protected abstract Task OnRewardRedeemedAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct);

    /// <summary>Реакция награды на обычное сообщение Twitch-чата.</summary>
    protected virtual Task OnTwitchChatMessageAsync(ChatEventMessageEvent message, CancellationToken ct) =>
        Task.CompletedTask;

    /// <summary>Реакция награды на сообщение, отправленное через channel-point reward.</summary>
    protected virtual Task OnTwitchRewardInputAsync(ChatEventMessageEvent message, CancellationToken ct) =>
        Task.CompletedTask;

    internal Task DispatchTwitchChatMessageAsync(ChatEventMessageEvent message, CancellationToken ct) =>
        OnTwitchChatMessageAsync(message, ct);

    internal Task DispatchTwitchRewardInputAsync(ChatEventMessageEvent message, CancellationToken ct) =>
        OnTwitchRewardInputAsync(message, ct);
}
