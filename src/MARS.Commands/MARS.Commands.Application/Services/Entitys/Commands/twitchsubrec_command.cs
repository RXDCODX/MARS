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
public class TwitchsubrecCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "twitchsubrec";

    public override string Description => "Пересоздаёт EventSub-подписки Twitch";

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
            var text = await grpcClient.Twitch.ResubscribeEventSubAsync(cancellationToken);
            return CommandResult.Ok(string.IsNullOrWhiteSpace(text) ? "EventSub-подписки пересозданы." : text);
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось пересоздать EventSub-подписки.", CommandErrorCode.TargetUnreachable);
        }
    }
}