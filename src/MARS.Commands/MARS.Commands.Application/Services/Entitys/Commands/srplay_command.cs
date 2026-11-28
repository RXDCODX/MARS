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
public class SrplayCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "srplay";

    public override string Description => "Возобновляет воспроизведение SoundRequest";

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
            var text = await grpcClient.SoundRequest.ResumePlaybackAsync(cancellationToken);
            return CommandResult.Ok(string.IsNullOrWhiteSpace(text) ? "Воспроизведение возобновлено." : text);
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось возобновить воспроизведение.", CommandErrorCode.TargetUnreachable);
        }
    }
}