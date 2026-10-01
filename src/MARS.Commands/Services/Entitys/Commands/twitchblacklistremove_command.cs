using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class twitchblacklistremove_command : BaseCommand
{
    public override string CommandName => "twitchblacklistremove";
    public override string Description =>
        "Команда для удаления пользователя из черного списка для пользования функциями твич алертов (и прочего)";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms =>
        [Platform.Api, Platform.Discord, Platform.Telegram, Platform.Twitch];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Type = CommandParameterType.String,
                Description = "@никнейм или twitchId пользователя",
                Name = "input",
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
