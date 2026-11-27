using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class RandommemCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "randommem";

    public override string Description => "Включает или выключает отправку случайных мемов";

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
            var enabled = parameters.TryGetValue("enabled", out var value) && bool.TryParse(value?.ToString(), out var parsed)
                ? parsed
                : true;

            await grpcClient.Telegram.Messages.SetRandomMemEnabledAsync(enabled, cancellationToken);
            return CommandResult.Ok(enabled ? "Отправка случайных мемов включена." : "Отправка случайных мемов выключена.");
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось изменить отправку случайных мемов.", CommandErrorCode.TargetUnreachable);
        }
    }
}