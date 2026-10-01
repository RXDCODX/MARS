using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class randomshorts_command : BaseCommand
{
    public override string CommandName => "randomshorts";
    public override string Description => "Получить случайный Short с ютуба";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                DefaultValue = "Shorts dank meme",
                Description = "Query для ютуба",
                Name = "query",
                Required = false,
                Type = CommandParameterType.String,
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
