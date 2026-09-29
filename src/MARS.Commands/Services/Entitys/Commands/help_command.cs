using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class HelpCommand : BaseCommand
{
    public override string CommandName => "help";
    public override string Description => "Показывает справку по возможностям бота и форматам медиа, или информацию о конкретной команде";
    public override bool IsAdminCommand => false;
    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api, Platform.Twitch];

    public override CommandVisibility Visibility => CommandVisibility.All;

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "commandName",
                Description = "Название команды для получения справки",
                Type = "string",
                Required = false,
            },
        ];

    public override Task<string> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        // Stub: external dependencies not available in Commands microservice
        return Task.FromResult("Команда недоступна в текущей конфигурации (микросервис)");
    }
}
