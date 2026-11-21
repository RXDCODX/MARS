using System.Collections.Concurrent;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.Services;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Rewards;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._1_RandomReward;

/// <summary>
/// Обработчик награды «Случайная награда»: выбирает случайную награду из пула
/// и переопубликовывает её событие погашения.
/// </summary>
public class RandomRewardHandler(
    ILogger<RandomRewardHandler> logger,
    IEnumerable<IRewardAlertHandler> handlers
) : RewardHandlerBase
{
    public override string RewardCode => "random_reward";

    private readonly ConcurrentDictionary<string, DateTimeOffset> _userCooldowns = new();

    private static readonly TimeSpan DefaultCooldown = TimeSpan.FromSeconds(60);

    private readonly HashSet<string> _pendingCodes = BuildPendingCodes(handlers);

    private static HashSet<string> BuildPendingCodes(IEnumerable<IRewardAlertHandler> handlers)
    {
        try
        {
            return new HashSet<string>(
                handlers.OfType<PendingRewardAlertHandler>().Select(h => h.RewardCode),
                StringComparer.Ordinal
            );
        }
        catch
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }
    }

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var user = rewardEvent.User;
        var userId = rewardEvent.UserId;

        // Per-user cooldown.
        var cooldown = RewardCatalog.Get(RewardCode).UserCooldown ?? DefaultCooldown;
        var now = DateTimeOffset.UtcNow;
        if (_userCooldowns.TryGetValue(userId, out var last) && now - last < cooldown)
        {
            var remaining = (int)(cooldown - (now - last)).TotalSeconds;
            await messageBus.PublishAsync(
                new ChatSendEvent
                {
                    Message =
                        $"@{rewardEvent.UserName}, подождите ещё {remaining} сек. до следующей случайной награды.",
                }
            );
            return;
        }

        _userCooldowns[userId] = now;

        var pool = RewardCatalog
            .All.Where(d => d.ExpectedCost > 0 && !d.ExcludedFromRandomPool)
            .Where(d => d.Code != RewardCode)
            .Where(d => !_pendingCodes.Contains(d.Code))
            .ToList();

        if (pool.Count == 0)
        {
            await messageBus.PublishAsync(
                new ChatSendEvent { Message = "Пул случайных наград пуст." }
            );
            return;
        }

        var chosen = pool[Random.Shared.Next(pool.Count)];

        var cloned = new RewardRedeemedEvent
        {
            RewardId = rewardEvent.RewardId,
            RewardCode = chosen.Code,
            RewardTitle = chosen.LegacyTitle,
            Cost = chosen.ExpectedCost,
            UserId = rewardEvent.UserId,
            UserName = rewardEvent.UserName,
            UserInput = rewardEvent.UserInput,
            RedeemedAt = rewardEvent.RedeemedAt,
            MessageId = rewardEvent.MessageId,
            User = rewardEvent.User,
        };

        await messageBus.PublishAsync(cloned);

        await messageBus.PublishAsync(
            new ChatSendEvent
            {
                Message =
                    $"@{rewardEvent.UserName} активировал(а) случайную награду: {chosen.LegacyTitle}",
            }
        );

        logger.LogInformation(
            "RandomReward by {UserName} dispatched to {RewardCode}",
            rewardEvent.UserName,
            chosen.Code
        );
    }
}
