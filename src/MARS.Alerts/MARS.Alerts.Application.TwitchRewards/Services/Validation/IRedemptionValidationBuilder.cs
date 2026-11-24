using MARS.Alerts.Application.TwitchRewards.Services.Validation;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards.Services.Validation;

public interface IRedemptionValidationBuilder
{
    IRedemptionValidationBuilder RequireBroadcasterUserId(bool loud = false);
    IRedemptionValidationBuilder RequireCost(int cost, bool loud = false);
    IRedemptionValidationBuilder RequireRewardEnabled(Func<bool> isEnabled, bool loud = false);
    IRedemptionValidationBuilder RequireFollower(bool loud = true);
    Task<ValidationResult> ValidateWithResponseAsync(string userName);
}
