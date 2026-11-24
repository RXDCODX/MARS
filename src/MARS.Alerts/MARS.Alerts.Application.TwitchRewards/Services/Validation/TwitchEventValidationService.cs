using MARS.Alerts.Application.TwitchRewards.Services.Validation;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Twitch.Rest;

namespace MARS.Alerts.Application.TwitchRewards.Services.Validation;

public sealed class TwitchEventValidationService(
    ITwitchInternalClient twitchClient,
    ILogger<TwitchEventValidationService> logger
) : ITwitchEventValidationService
{
    public IRedemptionValidationBuilder ForRedemption(RewardRedeemedEvent rewardEvent) =>
        new RedemptionValidationBuilder(rewardEvent, twitchClient, logger);
}
