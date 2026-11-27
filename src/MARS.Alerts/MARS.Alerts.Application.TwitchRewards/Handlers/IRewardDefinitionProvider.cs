using MARS.Shared.Contracts.Alerts;

namespace MARS.Alerts.Application.TwitchRewards.Handlers;

/// <summary>
/// Предоставляет определение награды для синхронизации в MARS.Twitch.
/// </summary>
public interface IRewardDefinitionProvider
{
    RewardDefinitionSnapshot CreateDefinitionSnapshot(bool isActive);
}
