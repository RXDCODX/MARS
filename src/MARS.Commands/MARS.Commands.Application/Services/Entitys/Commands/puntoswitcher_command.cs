using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class PuntoswitcherCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "puntoswitcher";

    public override string Description => "Включает или выключает Punto Switcher";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms => [Platform.Twitch, Platform.Telegram, Platform.Api];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var enabled = await grpcClient.Twitch.TogglePuntoSwitcherAsync(cancellationToken);
            return CommandResult.Ok(enabled ? "Punto Switcher включён." : "Punto Switcher выключен.");
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось изменить Punto Switcher.", CommandErrorCode.TargetUnreachable);
        }
    }
}