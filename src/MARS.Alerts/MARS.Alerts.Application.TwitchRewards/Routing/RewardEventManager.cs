using MARS.Platform.Infrastructure.Observability.Telemetry;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Rewards;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards;

public sealed class RewardEventManager
{
    private readonly IReadOnlyDictionary<string, IRewardAlertHandler> _rewardHandlers;
    private readonly IReadOnlyList<RewardHandlerBase> _eventHandlers;

    public RewardEventManager(
        IEnumerable<IRewardAlertHandler> handlers,
        ILogger<RewardEventManager> logger
    )
    {
        var registered = new Dictionary<string, IRewardAlertHandler>(StringComparer.Ordinal);
        var duplicates = new List<string>();
        var allHandlers = handlers.ToArray();

        foreach (var handler in allHandlers)
        {
            if (!registered.TryAdd(handler.RewardCode, handler))
            {
                duplicates.Add(handler.RewardCode);
            }
        }

        if (duplicates.Count > 0)
        {
            logger.LogWarning(
                "Duplicate reward alert handlers registered for codes: {Codes}. First registration wins.",
                string.Join(", ", duplicates)
            );
        }

        _rewardHandlers = registered;
        _eventHandlers = allHandlers.OfType<RewardHandlerBase>().ToArray();
    }

    public async Task HandleRewardRedeemedAsync(RewardRedeemedEvent message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        var rewardCode = message.RewardCode;
        if (string.IsNullOrWhiteSpace(rewardCode))
        {
            MarsMetrics.RabbitMqUnhandledMessages.Add(
                1,
                new KeyValuePair<string, object?>("reward_code", string.Empty)
            );
            return;
        }

        RewardCatalog.ByCode.TryGetValue(rewardCode, out var definition);
        var group = definition?.Group;

        if (group is null)
        {
            if (!_rewardHandlers.TryGetValue(rewardCode, out var handler))
            {
                MarsMetrics.RabbitMqUnhandledMessages.Add(
                    1,
                    new KeyValuePair<string, object?>("reward_code", rewardCode)
                );
                return;
            }

            if (!await handler.Activation.IsActiveAsync(message, ct))
            {
                return;
            }

            await handler.HandleAsync(message, ct);
            return;
        }

        var groupCodes = RewardCatalog
            .All.Where(item => item.Group == group)
            .Select(item => item.Code)
            .ToArray();

        var activeHandlers = new List<IRewardAlertHandler>();
        foreach (var code in groupCodes)
        {
            if (!_rewardHandlers.TryGetValue(code, out var handler))
            {
                continue;
            }

            if (await handler.Activation.IsActiveAsync(message, ct))
            {
                activeHandlers.Add(handler);
            }
        }

        if (activeHandlers.Count == 0)
        {
            return;
        }

        if (activeHandlers.Count > 1)
        {
            MarsMetrics.RabbitMqUnhandledMessages.Add(
                1,
                new KeyValuePair<string, object?>("reward_code", $"conflict:{group}")
            );
            return;
        }

        await activeHandlers[0].HandleAsync(message, ct);
    }

    public Task HandleTwitchChatMessageAsync(ChatEventMessageEvent message, CancellationToken ct) =>
        DispatchAsync(handler => handler.DispatchTwitchChatMessageAsync(message, ct));

    public Task HandleTwitchRewardInputAsync(
        RewardInputChatMessageEvent message,
        CancellationToken ct
    )
    {
        if (message?.Message is null)
        {
            return Task.CompletedTask;
        }

        return DispatchAsync(handler =>
            handler.DispatchTwitchRewardInputAsync(message.Message, ct)
        );
    }

    public Task HandleTwitchEventAsync(EventSubRawEvent message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        return DispatchAsync(handler => handler.DispatchTwitchEventAsync(message, ct));
    }

    private async Task DispatchAsync(Func<RewardHandlerBase, Task> dispatch)
    {
        foreach (var handler in _eventHandlers)
        {
            await dispatch(handler);
        }
    }
}
