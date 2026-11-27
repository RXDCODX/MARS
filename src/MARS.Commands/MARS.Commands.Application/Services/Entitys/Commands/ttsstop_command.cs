using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.Tts.Control;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class TtsStopCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "ttsstop";

    public override string Description => "Остановить воспроизведение TTS";

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
            await grpcClient.Tts.SetMuteStateAsync(true, 0, cancellationToken);
            return CommandResult.Ok("TTS остановлен.");
        }
        catch (RpcException)
        {
            return CommandResult.Fail(
                "Не удалось остановить TTS.",
                CommandErrorCode.TargetUnreachable
            );
        }
    }
}