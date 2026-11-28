using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

[ExcludeFromCodeCoverage]
public class SrpauseCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "srpause";

    public override string Description => "Ставит воспроизведение SoundRequest на паузу";

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
            var text = await grpcClient.SoundRequest.PausePlaybackAsync(cancellationToken);
            return CommandResult.Ok(
                string.IsNullOrWhiteSpace(text) ? "Воспроизведение поставлено на паузу." : text
            );
        }
        catch (RpcException)
        {
            return CommandResult.Fail(
                "Не удалось поставить воспроизведение на паузу.",
                CommandErrorCode.TargetUnreachable
            );
        }
    }
}
