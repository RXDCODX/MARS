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
public class RollWaifuCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "rollwaifu";
    public override string Description => "Выполняет вайфу-ролл для пользователя";
    public override bool IsAdminCommand => true;
    public override string[] Aliases => ["waifuroll"];
    public override Platform[] AvailablePlatforms =>
        [Platform.Telegram, Platform.Api, Platform.Twitch];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "username",
                Description = "Имя пользователя",
                Type = CommandParameterType.String,
                Required = true,
            },
        ];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        if (!parameters.TryGetValue("username", out var usernameObj))
        {
            return CommandResult.Fail(
                "Необходимо указать имя пользователя",
                CommandErrorCode.BadArguments
            );
        }

        var username = (usernameObj as string ?? "").TrimStart('@');

        if (string.IsNullOrWhiteSpace(username))
        {
            return CommandResult.Fail(
                "Необходимо указать имя пользователя",
                CommandErrorCode.BadArguments
            );
        }

        string? message;
        try
        {
            message = await grpcClient.WaifuGacha.RollWaifuAsync(username, cancellationToken);
        }
        catch (RpcException)
        {
            message = null;
        }
        if (string.IsNullOrWhiteSpace(message))
        {
            return CommandResult.Fail(
                "Не удалось выполнить вайфу-ролл",
                CommandErrorCode.TargetUnreachable
            );
        }

        return CommandResult.Ok(message);
    }
}
