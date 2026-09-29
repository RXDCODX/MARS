using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class WhitelistCommand : BaseCommand
{
    public override string CommandName => "whitelist";
    public override string Description => "Показывает список пользователей с доступом к PyroAlerts";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api, Platform.Twitch];

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
