using MARS.Alerts.Application.TwitchRewards.Services.Validation;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards.Services.Validation;

public interface ITwitchEventValidationService
{
    IRedemptionValidationBuilder ForRedemption(RewardRedeemedEvent rewardEvent);
}
