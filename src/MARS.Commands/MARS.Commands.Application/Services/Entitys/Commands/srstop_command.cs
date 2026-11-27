using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class SrstopCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "srstop";

    public override string Description => "Останавливает воспроизведение SoundRequest";

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
            var text = await grpcClient.SoundRequest.StopPlaybackAsync(cancellationToken);
            return CommandResult.Ok(string.IsNullOrWhiteSpace(text) ? "Воспроизведение остановлено." : text);
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось остановить воспроизведение.", CommandErrorCode.TargetUnreachable);
        }
    }
}