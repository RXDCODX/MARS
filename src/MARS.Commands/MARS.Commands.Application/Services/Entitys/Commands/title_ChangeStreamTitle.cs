using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class TitleChangeStreamTitleCommand : BaseCommand
{
    public override string CommandName => "title";
    public override string Description => "Смена названия трансляции Twitch";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms =>
        [Platform.Twitch, Platform.Telegram, Platform.Api];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "новое_название",
                Description = "Новое название для трансляции",
                Type = CommandParameterType.String,
                Required = false,
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
