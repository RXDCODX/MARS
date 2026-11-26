using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class unmutesound_command(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "unmutesound";
    public override string Description => "Включить звук на компухтере";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        int count;
        try
        {
            count = await grpcClient.AudioController.UnmuteAsync(cancellationToken);
        }
        catch (RpcException)
        {
            return CommandResult.Fail(
                "Комплюхтер не удалось размутить: аудиоконтроллер недоступен.",
                CommandErrorCode.TargetUnreachable
            );
        }

        return CommandResult.Ok($"Комплюхтер размучен успешно. Разблокировано сессий: {count}.");
    }
}
