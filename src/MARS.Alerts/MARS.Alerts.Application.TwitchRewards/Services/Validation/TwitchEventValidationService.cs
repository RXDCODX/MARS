using MARS.Shared.Contracts.Messaging;
using Wolverine;

namespace MARS.Alerts.Application.TwitchRewards.Services;

public sealed class TwitchEventValidationService(
    IMessageBus messageBus,
    ILogger<TwitchEventValidationService> logger
) : ITwitchEventValidationService
{
    public IRedemptionValidationBuilder ForRedemption(RewardRedeemedEvent rewardEvent) =>
        new RedemptionValidationBuilder(rewardEvent, messageBus, logger);
}
