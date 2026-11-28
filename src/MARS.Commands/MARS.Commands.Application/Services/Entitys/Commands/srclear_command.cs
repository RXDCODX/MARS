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
public class SrclearCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "srclear";

    public override string Description => "Очищает очередь SoundRequest";

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
            var text = await grpcClient.SoundRequest.ClearQueueAsync(cancellationToken);
            return CommandResult.Ok(string.IsNullOrWhiteSpace(text) ? "Очередь очищена." : text);
        }
        catch (RpcException)
        {
            return CommandResult.Fail(
                "Не удалось очистить очередь.",
                CommandErrorCode.TargetUnreachable
            );
        }
    }
}
