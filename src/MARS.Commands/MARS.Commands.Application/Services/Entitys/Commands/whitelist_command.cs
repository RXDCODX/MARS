using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class WhitelistCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "whitelist";

    public override string Description => "Показывает пользователей с доступом PyroAlerts";

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
            var users = await grpcClient.Telegram.Messages.GetWhitelistAsync(cancellationToken);
            return CommandResult.Ok(users.Count == 0 ? "Список пуст." : string.Join("\n", users));
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось получить whitelist.", CommandErrorCode.TargetUnreachable);
        }
    }
}