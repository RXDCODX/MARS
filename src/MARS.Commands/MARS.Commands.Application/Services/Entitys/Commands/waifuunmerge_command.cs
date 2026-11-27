using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class WaifuUnmergeCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "waifuunmerge";
    public override string Description => "Развести супруга";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms =>
        [Platform.Telegram, Platform.Api, Platform.Twitch];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var twitchId = TwitchCaller.ResolveId(parameters);

        if (string.IsNullOrWhiteSpace(twitchId))
        {
            return CommandResult.Fail(
                "Не удалось определить пользователя",
                CommandErrorCode.BadArguments
            );
        }

        string? message;
        try
        {
            message = await grpcClient.WaifuGacha.UnmergeHusbandAsync(twitchId, cancellationToken);
        }
        catch (RpcException)
        {
            message = null;
        }
        if (string.IsNullOrWhiteSpace(message))
        {
            return CommandResult.Fail("Не удалось развести", CommandErrorCode.TargetUnreachable);
        }

        return CommandResult.Ok(message);
    }
}
