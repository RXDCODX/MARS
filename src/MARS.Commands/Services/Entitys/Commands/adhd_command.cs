using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class AdhdCommand : BaseCommand
{
    public override string CommandName => "adhd";
    public override string Description => "Активирует ADHD эффект на указанное количество секунд";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms => [Platform.Api, Platform.Telegram, Platform.Twitch];

    public override string[] Aliases => ["adhdactivate", "adhdstart"];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "seconds",
                Description = "Количество секунд для активации ADHD эффекта",
                Type = "int",
                Required = true,
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
