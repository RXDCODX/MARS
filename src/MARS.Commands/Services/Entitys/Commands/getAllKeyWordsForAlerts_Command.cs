using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class GetAllKeyWordsForAlertsCommand : BaseCommand
{
    public override string CommandName => "getAllKeyWordsForAlerts";
    public override string Description => "Получить все ключевые слова для активации алертов";
    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api];

    public override Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        // Stub: database access not available in Commands microservice
        return Task.FromResult(
            CommandResult.Ok("Команда недоступна в текущей конфигурации (нет подключения к БД)")
        );
    }
}
