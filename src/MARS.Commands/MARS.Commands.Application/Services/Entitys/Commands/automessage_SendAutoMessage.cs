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
public class AutomessageCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "automessage";

    public override string Description => "Отправляет одно автоматическое сообщение";

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
            await grpcClient.Twitch.ExecuteAutoMessageAsync(cancellationToken);
            return CommandResult.Ok("Авто-сообщение отправлено.");
        }
        catch (RpcException)
        {
            return CommandResult.Fail(
                "Не удалось отправить авто-сообщение.",
                CommandErrorCode.TargetUnreachable
            );
        }
    }
}
