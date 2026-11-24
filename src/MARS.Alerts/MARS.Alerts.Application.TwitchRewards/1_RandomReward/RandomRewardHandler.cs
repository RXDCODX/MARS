using System.Collections.Concurrent;
using System.Drawing;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Services.Validation;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Twitch.Rest;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._1_RandomReward;

/// <summary>
/// «Случайная награда»: выбирает случайную стоимость среди зарегистрированных
/// обработчиков и переопубликует редемпшен с этой стоимостью. В монолите пул
/// собирался рефлексией по delegates EventSub; здесь источник — DI-набор
/// обработчиков, что даёт ту же динамику без reflection.
/// </summary>
public sealed class RandomRewardHandler(
    ILogger<RandomRewardHandler> logger,
    IEnumerable<IRewardAlertHandler> handlers,
    ITwitchInternalClient twitchClient,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    private const int RecentCostsLimit = 7;
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);

    private readonly ConcurrentDictionary<string, DateTimeOffset> _userCooldowns = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly List<int> _recentCosts = [];
    private readonly Lock _recentCostsLock = new();

    public override int RewardCost { get; } = 1;
    public override string AlertDisplayName { get; set; } = "🎲 Случайная награда!";
    public override string AlertDescription { get; set; } =
        "🎲 Активирует 1 случайную награду канала!";
    public override Color Color { get; set; } = Color.FromArgb(255, 255, 255);
    public override int Cost { get; init; } = 1;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        SubscribeOnStart(
            lifetime,
            rewardEventManager,
            onRedeem: ValidatedRedeem(validator, OnRewardRedeemedAsync, Cost, Activation)
        );
        return Task.CompletedTask;
    }

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var userId = rewardEvent.UserId;
        var now = DateTimeOffset.UtcNow;

        if (_userCooldowns.TryGetValue(userId, out var last) && now - last < Cooldown)
        {
            var remaining = (int)(Cooldown - (now - last)).TotalSeconds;
            await twitchClient.SendChatMessageAsync(
                new TwitchChatSendRequest
                {
                    Message =
                        $"@{rewardEvent.UserName}, подождите ещё {remaining} сек. до следующей случайной награды.",
                }
            );
            return;
        }

        _userCooldowns[userId] = now;

        var namesByCost = handlers
            .Where(h => h.RewardCost > 0 && h.RewardCost != RewardCost)
            .GroupBy(h => h.RewardCost)
            .ToDictionary(g => g.Key, g => g.First().AlertDisplayName);

        var candidates = namesByCost.Keys.ToHashSet();
        candidates.Remove(RewardCost);

        lock (_recentCostsLock)
        {
            foreach (var recent in _recentCosts)
                candidates.Remove(recent);
        }

        if (candidates.Count == 0)
        {
            await twitchClient.SendChatMessageAsync(
                new TwitchChatSendRequest { Message = "Нет доступных случайных наград :(" }
            );
            return;
        }

        var chosenCost = candidates.ElementAt(Random.Shared.Next(candidates.Count));

        var chosenTitle =
            handlers
                .Where(h => h.RewardCost == chosenCost)
                .FirstOrDefault(h =>
                    h.Activation.IsActiveForSchedulingAsync(ct).GetAwaiter().GetResult()
                )
                ?.AlertDisplayName
            ?? handlers.FirstOrDefault(h => h.RewardCost == chosenCost)?.AlertDisplayName
            ?? "Случайная награда";

        lock (_recentCostsLock)
        {
            _recentCosts.Add(chosenCost);
            while (_recentCosts.Count > RecentCostsLimit)
                _recentCosts.RemoveAt(0);
        }

        var cloned = new RewardRedeemedEvent
        {
            RewardId = rewardEvent.RewardId,
            RewardCode = $"cost_{chosenCost}",
            RewardTitle = chosenTitle,
            Cost = chosenCost,
            UserId = rewardEvent.UserId,
            UserName = rewardEvent.UserName,
            UserInput = rewardEvent.UserInput,
            RedeemedAt = rewardEvent.RedeemedAt,
            MessageId = rewardEvent.MessageId,
            User = rewardEvent.User,
            BroadcasterUserId = rewardEvent.BroadcasterUserId,
            IsFollower = rewardEvent.IsFollower,
        };

        await rewardEventManager.HandleRewardRedeemedAsync(cloned, ct);

        await twitchClient.SendChatMessageAsync(
            new TwitchChatSendRequest
            {
                Message =
                    $"@{rewardEvent.UserName} активировал(а) случайную награду: {cloned.RewardTitle} за {chosenCost} баллов!",
            }
        );

        logger.LogInformation(
            "RandomReward by {UserName} dispatched to cost {Cost}",
            rewardEvent.UserName,
            chosenCost
        );
    }
}
