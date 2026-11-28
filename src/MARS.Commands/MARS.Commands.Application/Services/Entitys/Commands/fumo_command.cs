using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.Alerts.Control;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Commands.Application.Services.Entitys.Commands;

[ExcludeFromCodeCoverage]
public class FumoCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "fumo";

    public override string Description => "Отправляет фумо в чат";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms =>
        [Platform.Api, Platform.Telegram, Platform.Twitch];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "displayName",
                Description = "Имя пользователя",
                Type = CommandParameterType.String,
                Required = true,
            },
            new()
            {
                Name = "color",
                Description = "Цвет (опционально)",
                Type = CommandParameterType.String,
                Required = false,
            },
        ];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        if (!parameters.TryGetValue("displayName", out var displayNameObj))
        {
            return CommandResult.Fail("Необходимо указать имя пользователя", CommandErrorCode.BadArguments);
        }

        var displayName = displayNameObj?.ToString()?.Trim() ?? string.Empty;
        displayName = displayName.StartsWith('@') ? displayName[1..] : displayName;
        var color = parameters.TryGetValue("color", out var colorObj) ? colorObj?.ToString() : null;

        try
        {
            var response = await grpcClient.Alerts.InvokeCommandAsync(
                new InvokeAlertCommandRequest
                {
                    Fumo = new FumoAlertCommandPayload { DisplayName = displayName, Color = color },
                },
                cancellationToken
            );

            return CommandResult.Ok(string.IsNullOrWhiteSpace(response.Result) ? $"Фумо фрайдей с {displayName} объявлен!" : response.Result);
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось отправить фумо.", CommandErrorCode.TargetUnreachable);
        }
    }
}