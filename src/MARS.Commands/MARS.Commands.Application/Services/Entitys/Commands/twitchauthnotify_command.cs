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
public class TwitchauthnotifyCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "twitchauthnotify";

    public override string Description =>
        "Отправляет администраторам просьбу пройти авторизацию Twitch";

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
            var success = await grpcClient.Twitch.NotifyMissingTwitchTokenAsync(cancellationToken);
            return success
                ? CommandResult.Ok("Просьба пройти авторизацию Twitch отправлена.")
                : CommandResult.Fail(
                    "Не удалось отправить просьбу пройти авторизацию.",
                    CommandErrorCode.TargetUnreachable
                );
        }
        catch (RpcException)
        {
            return CommandResult.Fail(
                "Не удалось отправить просьбу пройти авторизацию.",
                CommandErrorCode.TargetUnreachable
            );
        }
    }
}
