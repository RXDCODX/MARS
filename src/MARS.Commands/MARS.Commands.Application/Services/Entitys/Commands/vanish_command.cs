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
public class VanishCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "vanish";

    public override string Description => "Скрывает сообщение пользователя на 1 секунду";

    public override bool IsAdminCommand => false;

    public override Platform[] AvailablePlatforms => [Platform.Twitch];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var userId = parameters.TryGetValue("userId", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return CommandResult.Fail("Не удалось определить пользователя.", CommandErrorCode.BadArguments);
        }

        try
        {
            var success = await grpcClient.Twitch.TimeoutUserAsync(userId, cancellationToken);
            return success
                ? CommandResult.Ok("Сообщение скрыто.")
                : CommandResult.Fail("Не удалось скрыть сообщение.", CommandErrorCode.TargetUnreachable);
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось скрыть сообщение.", CommandErrorCode.TargetUnreachable);
        }
    }
}