using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards.Services;

public interface ITwitchEventValidationService
{
    IRedemptionValidationBuilder ForRedemption(RewardRedeemedEvent rewardEvent);
}