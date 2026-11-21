using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class AdhdCommand : BaseCommand
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
                Required = true,
            },
        ];

    public override Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(
            CommandResult.Fail(
                "Команда ещё не реализована: нужные сервисы не подключены.",
                CommandErrorCode.NotImplemented
            )
        );
    }
}
