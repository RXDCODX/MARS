using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class SrCommand : BaseCommand
{
    public override string CommandName => "sr";
    public override string Description => "Добавить трек в очередь звуковых запросов";
    public override bool IsAdminCommand => false;
    public override Platform[] AvailablePlatforms => [Platform.Twitch, Platform.Api];

    public override string[] Aliases => ["soundrequest"];

    public override CommandVisibility Visibility => CommandVisibility.All;

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "query",
                Description = "URL видео или поисковый запрос",
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
