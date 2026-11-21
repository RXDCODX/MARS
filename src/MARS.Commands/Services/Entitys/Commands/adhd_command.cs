using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Shared.Contracts.Alerts;

namespace MARS.Commands.Services.Entitys.Commands;

public class AdhdCommand(IAlertsCommandClient alertsCommandClient) : BaseCommand
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

        var result = await alertsCommandClient.InvokeAsync(
            new AdhdAlertCommandPayload(seconds),
            cancellationToken
        );

        return CommandResult.Ok(result);
    }
}
