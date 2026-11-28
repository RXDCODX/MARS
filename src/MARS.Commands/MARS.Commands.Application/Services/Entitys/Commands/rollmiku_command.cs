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
public class RollMikuCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "rollmiku";
    public override string Description => "Выполняет ролл костюма Miku";
    public override bool IsAdminCommand => true;
    public override string[] Aliases => ["mikuroll"];
    public override Platform[] AvailablePlatforms =>
        [Platform.Telegram, Platform.Api, Platform.Twitch];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        string? message;
        try
        {
            message = await grpcClient.WaifuGacha.RollMikuAsync(cancellationToken);
        }
        catch (RpcException)
        {
            message = null;
        }
        if (string.IsNullOrWhiteSpace(message))
        {
            return CommandResult.Fail(
                "Не удалось выполнить Miku roll",
                CommandErrorCode.TargetUnreachable
            );
        }

        return CommandResult.Ok(message);
    }
}
