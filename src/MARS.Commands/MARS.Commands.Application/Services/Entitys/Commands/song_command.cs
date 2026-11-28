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
public class SongCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "song";

    public override string Description => "Показывает текущий трек SoundRequest";

    public override bool IsAdminCommand => false;

    public override Platform[] AvailablePlatforms =>
        [Platform.Twitch, Platform.Telegram, Platform.Api];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var text = await grpcClient.SoundRequest.GetCurrentSongAsync(cancellationToken);
            return CommandResult.Ok(
                string.IsNullOrWhiteSpace(text) ? "Сейчас ничего не играет." : text
            );
        }
        catch (RpcException)
        {
            return CommandResult.Fail(
                "Не удалось получить текущий трек.",
                CommandErrorCode.TargetUnreachable
            );
        }
    }
}
