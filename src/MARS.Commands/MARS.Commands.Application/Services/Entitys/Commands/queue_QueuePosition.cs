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
public class QueueCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "queue";

    public override string Description => "Показывает позицию пользователя в очереди SoundRequest";

    public override bool IsAdminCommand => false;

    public override Platform[] AvailablePlatforms => [Platform.Twitch, Platform.Api];

    public override CommandParameterInfo[] Parameters => [];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var userId = parameters.TryGetValue("userId", out var value)
            ? value?.ToString() ?? string.Empty
            : string.Empty;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return CommandResult.Fail(
                "Не удалось определить пользователя.",
                CommandErrorCode.BadArguments
            );
        }

        try
        {
            var text = await grpcClient.SoundRequest.GetUserQueuePositionAsync(
                userId,
                cancellationToken
            );
            return CommandResult.Ok(
                string.IsNullOrWhiteSpace(text) ? "Не удалось получить позицию в очереди." : text
            );
        }
        catch (RpcException)
        {
            return CommandResult.Fail(
                "Не удалось получить позицию в очереди.",
                CommandErrorCode.TargetUnreachable
            );
        }
    }
}
