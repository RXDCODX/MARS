using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class TtsfilterCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "ttsfilter";

    public override string Description => "Включает или выключает фильтр дублей TTS";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Twitch, Platform.Api];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var enabled = await grpcClient.Tts.ToggleFilterAsync(cancellationToken);
            return CommandResult.Ok(enabled ? "Фильтр TTS включён." : "Фильтр TTS выключен.");
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось изменить фильтр TTS.", CommandErrorCode.TargetUnreachable);
        }
    }
}