using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.Alerts.Control;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class MikubeamCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "mikubeam";

    public override string Description => "Досрочная активация MIKU MIKU BEAM эффекта";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms =>
        [Platform.Api, Platform.Telegram, Platform.Twitch];

    public override string[] Aliases => ["mikumikubeam", "mmb"];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        string result;
        try
        {
            var response = await grpcClient.Alerts.InvokeCommandAsync(
                new InvokeAlertCommandRequest { Mikubeam = new MikubeamAlertCommandPayload() },
                cancellationToken
            );
            result = response.Result;
        }
        catch (RpcException)
        {
            result = "Команда не выполнена: сервис недоступен.";
        }

        return CommandResult.Ok(result);
    }
}
