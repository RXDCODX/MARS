using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class mutesound_command(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "mutesound";
    public override string Description => "Выключить звук на компухтере";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        MARS.Shared.Contracts.Models.Audio.MuteMixerResponse result;
        try
        {
            result = await grpcClient.AudioController.MuteAsync(null, cancellationToken);
        }
        catch (RpcException)
        {
            return CommandResult.Fail(
                "Комплюхтер не удалось замутить: аудиоконтроллер недоступен.",
                CommandErrorCode.TargetUnreachable
            );
        }

        return CommandResult.Ok(
            $"Комплюхтер замучен успешно. Заглушено сессий: {result.MutedSessionCount}."
        );
    }
}
