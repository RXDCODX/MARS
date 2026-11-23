using System;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards.Routing;

/// <summary>
/// Диспетчер входящих Twitch-событий обработчиков наград. Не знает про
/// конкретные обработчики: каждый из них подписывается сам (через DI и
/// <see cref="RewardHandlerBase.SubscribeOnStart"/>) на его события, а менеджер
/// их вызывает.
/// </summary>
/// <remarks>
/// Обработчики вызываются по одному: сбой одного не прерывает остальных,
/// ошибка логируется и проглатывается, чтобы одна плохая награда не уронила
/// весь поток редемпшенов.
/// </remarks>
public sealed class RewardEventManager(ILogger<RewardEventManager>? logger = null)
    : IRewardEventManager
{
    private readonly ILogger<RewardEventManager> _logger =
        logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RewardEventManager>.Instance;

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

    public async Task HandleRewardRedeemedAsync(RewardRedeemedEvent message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        await InvokeAllAsync(OnTwitchRewardRedeem, message, ct, "OnTwitchRewardRedeem");
    }

    public Task HandleTwitchChatMessageAsync(ChatEventMessageEvent message, CancellationToken ct) =>
        InvokeAllAsync(OnTwitchMessage, message, ct, "OnTwitchMessage");

    public Task HandleTwitchRewardInputAsync(
        RewardInputChatMessageEvent message,
        CancellationToken ct
    ) => InvokeAllAsync(OnTwitchRewardInput, message, ct, "OnTwitchRewardInput");

    public Task HandleTwitchEventAsync(EventSubRawEvent message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        return InvokeAllAsync(OnTwitchEvent, message, ct, "OnTwitchEvent");
    }

    private async Task InvokeAllAsync<T>(
        Func<T, CancellationToken, Task> multicast,
        T message,
        CancellationToken ct,
        string eventName
    )
    {
        foreach (
            var handler in multicast.GetInvocationList().Cast<Func<T, CancellationToken, Task>>()
        )
        {
            try
            {
                await handler(message, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Обработчик {Event} упал, продолжаем с остальными", eventName);
            }
        }
    }
}
