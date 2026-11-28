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
public class TwitchblacklistremoveCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "twitchblacklistremove";

    public override string Description => "Удаляет пользователя из чёрного списка Twitch";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api, Platform.Twitch, Platform.Discord];

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
        var input = parameters.TryGetValue("input", out var value) ? value?.ToString()?.Trim() ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return CommandResult.Fail("Не указан пользователь.", CommandErrorCode.BadArguments);
        }

        try
        {
            var success = await grpcClient.Twitch.RemoveBlacklistedUserAsync(input, cancellationToken);
            return success
                ? CommandResult.Ok($"Пользователь {input} удалён из чёрного списка.")
                : CommandResult.Fail("Не удалось удалить пользователя из чёрного списка.", CommandErrorCode.TargetUnreachable);
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось удалить пользователя из чёрного списка.", CommandErrorCode.TargetUnreachable);
        }
    }
}