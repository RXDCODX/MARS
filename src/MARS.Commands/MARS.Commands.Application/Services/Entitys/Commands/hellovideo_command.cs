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
public class HellovideoCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "hellovideo";

    public override string Description => "Тестирует генерацию приветственного видео";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms =>
        [Platform.Twitch, Platform.Telegram, Platform.Api];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "name",
                Description = "Имя пользователя",
                Type = CommandParameterType.String,
                Required = true,
            },
            new()
            {
                Name = "color",
                Description = "Цвет эффекта",
                Type = CommandParameterType.String,
                Required = false,
                DefaultValue = "white",
            },
        ];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var name = parameters.TryGetValue("name", out var nameValue)
            ? nameValue?.ToString()?.Trim() ?? string.Empty
            : string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return CommandResult.Fail("Не указано имя.", CommandErrorCode.BadArguments);
        }

        var color =
            parameters.TryGetValue("color", out var colorValue)
            && !string.IsNullOrWhiteSpace(colorValue?.ToString())
                ? colorValue!.ToString()!.Trim()
                : "white";

        try
        {
            var result = await grpcClient.Twitch.TestHelloVideoAsync(
                name,
                color,
                cancellationToken
            );
            return string.IsNullOrWhiteSpace(result)
                ? CommandResult.Fail(
                    "Не удалось сгенерировать видео.",
                    CommandErrorCode.TargetUnreachable
                )
                : CommandResult.Ok(result);
        }
        catch (RpcException)
        {
            return CommandResult.Fail(
                "Не удалось сгенерировать видео.",
                CommandErrorCode.TargetUnreachable
            );
        }
    }
}
