using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class SysteminfoCommand : BaseCommand
{
    public override string CommandName => "systeminfo";
    public override string Description => "Показывает информацию о системе и сервисах";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api];

    public override string[] Aliases => ["sysinfo"];

    public override CommandVisibility Visibility => CommandVisibility.FullList;

    public override Task<string> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        // Stub: external dependencies not available in Commands microservice
        return Task.FromResult("Команда недоступна в текущей конфигурации (нет подключения к БД)");
    }
}
