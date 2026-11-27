using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class WtelegramstatusCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "wtelegramstatus";

    public override string Description => "Показывает статус WTelegram-клиента";

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
            var status = await grpcClient.Telegram.Messages.GetWTelegramStatusAsync(cancellationToken);
            return CommandResult.Ok(string.IsNullOrWhiteSpace(status) ? "Статус недоступен." : status);
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось получить статус WTelegram.", CommandErrorCode.TargetUnreachable);
        }
    }
}