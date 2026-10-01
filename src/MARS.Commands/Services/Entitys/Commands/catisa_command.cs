using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class CatisaCommand : BaseCommand
{
    public override string CommandName => "catisa";
    public override string Description => "Отправить сообщение на твич канал";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms => [Platform.Api, Platform.Telegram];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "channel",
                Description = "Канал для отправки сообщения",
                Type = CommandParameterType.String,
                Required = true,
            },
            new()
            {
                Name = "text",
                Description = "Текст для отправки сообщения",
                Type = CommandParameterType.String,
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
