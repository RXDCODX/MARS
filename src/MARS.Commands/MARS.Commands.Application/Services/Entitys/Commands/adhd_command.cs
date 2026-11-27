using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.Alerts.Control;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class AdhdCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "adhd";

    public override string Description => "Активирует ADHD эффект на указанное количество секунд";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms =>
        [Platform.Api, Platform.Telegram, Platform.Twitch];

    public override string[] Aliases => ["adhdactivate", "adhdstart"];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "seconds",
                Description = "Количество секунд для активации ADHD эффекта",
                Type = CommandParameterType.Int,
                Required = false,
            },
        ];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var seconds = 0;
        if (parameters.TryGetValue("seconds", out var secondsObj))
        {
            int.TryParse(secondsObj?.ToString(), out seconds);
        }

        if (seconds < 0)
        {
            seconds = 0;
        }

        string result;
        try
        {
            var response = await grpcClient.Alerts.InvokeCommandAsync(
                new InvokeAlertCommandRequest
                {
                    Adhd = new AdhdAlertCommandPayload { Seconds = seconds },
                },
                cancellationToken
            );
            result = response.Result;
        }
        catch (RpcException)
        {
            result = "Команда не выполнена: сервис недоступен.";
        }

        return CommandResult.Ok(result);
    }
}
