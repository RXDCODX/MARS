using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class ZonezeroCommand : BaseCommand
{
    public override string CommandName => "zonezero";
    public override string Description =>
        "Показывает информацию о ежедневных уведомлениях Zenless Zone Zero";
    public override bool IsAdminCommand => false;
    public override Platform[] AvailablePlatforms => [Platform.Telegram];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "chatId",
                Description = "Id телеграм чата",
                Type = CommandParameterType.Long,
                Required = true,
            },
            new()
            {
                Name = "username",
                Description = "Имя пользователя",
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
