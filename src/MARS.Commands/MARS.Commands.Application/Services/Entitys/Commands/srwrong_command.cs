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
public class SrwrongCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "srwrong";

    public override string Description => "Отменяет последний добавленный трек";

    public override bool IsAdminCommand => false;

    public override Platform[] AvailablePlatforms => [Platform.Twitch, Platform.Api];

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
            var text = await grpcClient.SoundRequest.CancelLastTrackAsync(
                userId,
                cancellationToken
            );
            return CommandResult.Ok(
                string.IsNullOrWhiteSpace(text) ? "Последний трек отменён." : text
            );
        }
        catch (RpcException)
        {
            return CommandResult.Fail(
                "Не удалось отменить последний трек.",
                CommandErrorCode.TargetUnreachable
            );
        }
    }
}
