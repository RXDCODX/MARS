using MARS.Alerts.Application.TwitchRewards.Services.Validation;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards.Services.Validation;

public sealed class TwitchEventValidationService(
    IMarsGrpcClient grpcClient,
    ILogger<TwitchEventValidationService> logger
) : ITwitchEventValidationService
{
    public IRedemptionValidationBuilder ForRedemption(RewardRedeemedEvent rewardEvent) =>
        new RedemptionValidationBuilder(rewardEvent, grpcClient, logger);
}
