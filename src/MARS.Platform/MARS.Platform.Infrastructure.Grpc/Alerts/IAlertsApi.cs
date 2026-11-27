using Google.Protobuf.WellKnownTypes;
using MARS.Shared.Contracts.Alerts.Control;

namespace MARS.Platform.Infrastructure.Grpc.Alerts;

public interface IAlertsApi
{
    Task<InvokeAlertCommandResponse> InvokeCommandAsync(
        InvokeAlertCommandRequest request,
        CancellationToken cancellationToken = default
    );

    Task<RewardDefinitionsResponse> GetRewardDefinitionsAsync(
        CancellationToken cancellationToken = default
    );
}
