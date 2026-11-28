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
public class TwitchblacklistaddCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "twitchblacklistadd";

    public override string Description => "Добавляет пользователя в чёрный список Twitch";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms =>
        [Platform.Telegram, Platform.Api, Platform.Twitch, Platform.Discord];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "input",
                Description = "Логин или Twitch ID пользователя",
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
        var input = parameters.TryGetValue("input", out var value)
            ? value?.ToString()?.Trim() ?? string.Empty
            : string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return CommandResult.Fail("Не указан пользователь.", CommandErrorCode.BadArguments);
        }

        try
        {
            var success = await grpcClient.Twitch.AddBlacklistedUserAsync(input, cancellationToken);
            return success
                ? CommandResult.Ok($"Пользователь {input} добавлен в чёрный список.")
                : CommandResult.Fail(
                    "Не удалось добавить пользователя в чёрный список.",
                    CommandErrorCode.TargetUnreachable
                );
        }
        catch (RpcException)
        {
            return CommandResult.Fail(
                "Не удалось добавить пользователя в чёрный список.",
                CommandErrorCode.TargetUnreachable
            );
        }
    }
}
