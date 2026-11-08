using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using MARS.WaifuGacha.Configuration;
using MARS.WaifuGacha.Entities;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MARS.WaifuGacha.Services;

/// <summary>
/// Лисенер наград: при погашении награды стоимостью не менее порога регистрирует
/// ожидание уведомления о завершении кулдауна ролла.
/// </summary>
public sealed class RollCooldownRewardListener(
    RollCooldownNotificationState state,
    RollCooldownService cooldownService,
    RollCooldownConfigurationService cooldownConfiguration,
    ILogger<RollCooldownRewardListener> logger
) : MarsEventListener<RewardRedeemedEvent>
{
    private const int RollCost = 4;

    private static readonly Dictionary<string, string> RollTypeNames = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        [RootStateKeys.WaifuRollType] = "WaifuRoll",
        [RootStateKeys.MikuRollType] = "MikuRoll",
        [RootStateKeys.FumoRollType] = "FumoRoll",
        [RootStateKeys.FrogRollType] = "FrogRoll",
    };

    public override async Task Handle(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        if (rewardEvent is null || string.IsNullOrWhiteSpace(rewardEvent.UserId))
        {
            return;
        }

        if (rewardEvent.Cost < RollCost)
        {
            return;
        }

        var rollType = ResolveRollType(rewardEvent.RewardTitle);

        if (rollType is null)
        {
            return;
        }

        var cooldown = await cooldownConfiguration.GetCooldownAsync(rollType, ct);

        var (allowed, remaining) = await cooldownService.CheckAndUpdateCooldownAsync(
            rewardEvent.UserId,
            rollType,
            cooldown,
            ct
        );

        if (!allowed)
        {
            logger.LogInformation(
                "Ролл {RollType} для {UserId} отклонён: кулдаун активен ещё {Remaining}",
                rollType,
                rewardEvent.UserId,
                remaining
            );
            return;
        }

        await state.AddPendingAsync(rewardEvent.UserId, rollType, cooldown, ct);

        logger.LogDebug(
            "Добавлен в ожидание уведомления: {UserId} ({RollType}), кулдаун закончится: {CooldownEnd}",
            rewardEvent.UserId,
            rollType,
            DateTime.UtcNow + cooldown
        );
    }

    private static string? ResolveRollType(string rewardTitle)
    {
        if (string.IsNullOrWhiteSpace(rewardTitle))
        {
            return null;
        }

        var title = rewardTitle.Trim().ToLowerInvariant();

        if (title.Contains("fumo", StringComparison.Ordinal))
        {
            return RootStateKeys.FumoRollType;
        }

        if (title.Contains("miku", StringComparison.Ordinal))
        {
            return RootStateKeys.MikuRollType;
        }

        if (
            title.Contains("frog", StringComparison.Ordinal)
            || title.Contains("жаба", StringComparison.Ordinal)
        )
        {
            return RootStateKeys.FrogRollType;
        }

        if (
            title.Contains("waifu", StringComparison.Ordinal)
            || title.Contains("вайфу", StringComparison.Ordinal)
        )
        {
            return RootStateKeys.WaifuRollType;
        }

        return null;
    }
}
