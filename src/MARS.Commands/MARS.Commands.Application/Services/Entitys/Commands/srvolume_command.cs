using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class SrvolumeCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "srvolume";

    public override string Description => "Показывает или меняет громкость SoundRequest";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms => [Platform.Twitch];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "volume",
                Description = "Громкость от 0 до 100",
                Type = CommandParameterType.Int,
                Required = false,
            },
        ];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        int? volume = null;
        if (parameters.TryGetValue("volume", out var value) && int.TryParse(value?.ToString(), out var parsed))
        {
            volume = Math.Clamp(parsed, 0, 100);
        }
        else if (parameters.ContainsKey("volume"))
        {
            return CommandResult.Fail("Громкость должна быть числом от 0 до 100.", CommandErrorCode.BadArguments);
        }

        try
        {
            var text = await grpcClient.SoundRequest.SetVolumeAsync(volume, cancellationToken);
            return CommandResult.Ok(string.IsNullOrWhiteSpace(text) ? "Громкость обновлена." : text);
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось изменить громкость.", CommandErrorCode.TargetUnreachable);
        }
    }
}