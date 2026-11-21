using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
<<<<<<< HEAD:src/MARS.Commands/MARS.Commands.Application/Services/Entitys/Commands/fumo_command.cs
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
=======
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Shared.Contracts.Alerts;
>>>>>>> origin/main:src/MARS.Commands/Services/Entitys/Commands/adhd_command.cs

namespace MARS.Commands.Application.Services.Entitys.Commands;

<<<<<<< HEAD:src/MARS.Commands/MARS.Commands.Application/Services/Entitys/Commands/fumo_command.cs
public class FumoCommand : BaseCommand
{
    public override string CommandName => "fumo";
    public override string Description => "Отправляет фумо в чат";
=======
public class AdhdCommand(IAlertsCommandClient alertsCommandClient) : BaseCommand
{
    public override string CommandName => "adhd";

    public override string Description => "Активирует ADHD эффект на указанное количество секунд";

>>>>>>> origin/main:src/MARS.Commands/Services/Entitys/Commands/adhd_command.cs
    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms =>
        [Platform.Api, Platform.Telegram, Platform.Twitch];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
<<<<<<< HEAD:src/MARS.Commands/MARS.Commands.Application/Services/Entitys/Commands/fumo_command.cs
                Name = "displayName",
                Description = "Имя пользователя",
                Type = CommandParameterType.String,
                Required = true,
=======
                Name = "seconds",
                Description = "Количество секунд для активации ADHD эффекта",
                Type = CommandParameterType.Int,
                Required = false,
>>>>>>> origin/main:src/MARS.Commands/Services/Entitys/Commands/adhd_command.cs
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
