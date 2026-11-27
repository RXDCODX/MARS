using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class TwitchchannelreconnectCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "twitchchannelreconnect";

    public override string Description => "Переподключает Twitch-чат";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var success = await grpcClient.Twitch.ReconnectAsync(cancellationToken);
            return success
                ? CommandResult.Ok("Twitch-чат переподключён.")
                : CommandResult.Fail("Не удалось переподключить Twitch-чат.", CommandErrorCode.TargetUnreachable);
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось переподключить Twitch-чат.", CommandErrorCode.TargetUnreachable);
        }
    }
}