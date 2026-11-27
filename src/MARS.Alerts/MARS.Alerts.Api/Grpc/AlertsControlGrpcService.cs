using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MARS.Alerts.Application.TwitchRewards;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Shared.Contracts.Alerts.Control;
using Microsoft.Extensions.Logging;
using DomainAlerts = MARS.Shared.Contracts.Alerts;
using ProtoAlerts = MARS.Shared.Contracts.Alerts.Control;

namespace MARS.Alerts.Api.Grpc;

public sealed class AlertsControlGrpcService(
    IEnumerable<IRewardAlertHandler> handlers,
    ILogger<AlertsControlGrpcService> logger
) : AlertsControlService.AlertsControlServiceBase
{
    public override async Task<InvokeAlertCommandResponse> InvokeCommand(
        InvokeAlertCommandRequest request,
        ServerCallContext context
    )
    {
        DomainAlerts.AlertCommandPayload? payload = request.PayloadCase switch
        {
            InvokeAlertCommandRequest.PayloadOneofCase.Adhd =>
                new DomainAlerts.AdhdAlertCommandPayload(request.Adhd.Seconds),
            InvokeAlertCommandRequest.PayloadOneofCase.Mikubeam =>
                new DomainAlerts.MikubeamAlertCommandPayload(),
            _ => null,
        };

        if (payload is null)
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, "Команда не поддерживается.")
            );
        }

        var handler = handlers.FirstOrDefault(h => h.CommandPayloadType == payload.GetType());
        if (handler is null)
        {
            logger.LogWarning("Не найден обработчик для payload {Type}", payload.GetType().Name);
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, "Команда не поддерживается.")
            );
        }

        try
        {
            var result = await handler.InvokeFromCommandAsync(payload, context.CancellationToken);
            return new InvokeAlertCommandResponse { Result = result };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка вызова команды алерта");
            throw new RpcException(new Status(StatusCode.Internal, $"Ошибка: {ex.Message}"));
        }
    }

    public override Task<RewardDefinitionsResponse> GetRewardDefinitions(
        Google.Protobuf.WellKnownTypes.Empty request,
        ServerCallContext context
    )
    {
        var list = new List<ProtoAlerts.RewardDefinitionSnapshot>();
        foreach (var handler in handlers)
        {
            var isActive = handler
                .Activation.IsActiveForSchedulingAsync(context.CancellationToken)
                .GetAwaiter()
                .GetResult();
            list.Add(
                new ProtoAlerts.RewardDefinitionSnapshot
                {
                    Cost = handler.RewardCost,
                    Title = handler.AlertDisplayName,
                    Prompt = handler.AlertDescription,
                    BackgroundColor =
                        $"#{handler.Color.R:X2}{handler.Color.G:X2}{handler.Color.B:X2}",
                    IsUserInputRequired = handler.IsUserInputRequired,
                    IsActive = isActive,
                }
            );
        }

        return Task.FromResult(new RewardDefinitionsResponse { Definitions = { list } });
    }
}
