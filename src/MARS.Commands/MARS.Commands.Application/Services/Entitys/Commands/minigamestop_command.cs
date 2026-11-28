using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Commands.Application.Services.Entitys.Commands;

[ExcludeFromCodeCoverage]
public class MinigamestopCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "minigamestop";

    public override string Description => "Останавливает все активные мини-игры";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms => [Platform.Twitch];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await grpcClient.Twitch.CancelMiniGamesAsync(cancellationToken);
            return CommandResult.Ok("Все мини-игры остановлены.");
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось остановить мини-игры.", CommandErrorCode.TargetUnreachable);
        }
    }
}