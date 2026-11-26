using Google.Protobuf.WellKnownTypes;
using Grpc.Net.ClientFactory;
using MARS.Shared.Contracts.Alerts.Control;

namespace MARS.Platform.Infrastructure.Grpc.Alerts;

internal sealed class AlertsApi(GrpcClientFactory clientFactory) : IAlertsApi
{
    public Task<InvokeAlertCommandResponse> InvokeCommandAsync(
        InvokeAlertCommandRequest request,
        CancellationToken cancellationToken = default
    ) =>
        clientFactory
            .CreateClient<AlertsControlService.AlertsControlServiceClient>("Alerts")
            .InvokeCommandAsync(request, cancellationToken: cancellationToken)
            .ResponseAsync;

    public Task<RewardDefinitionsResponse> GetRewardDefinitionsAsync(
        CancellationToken cancellationToken = default
    ) =>
        clientFactory
            .CreateClient<AlertsControlService.AlertsControlServiceClient>("Alerts")
            .GetRewardDefinitionsAsync(new Empty(), cancellationToken: cancellationToken)
            .ResponseAsync;
}
