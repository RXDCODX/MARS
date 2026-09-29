using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class WaifuunmergeCommand : BaseCommand
{
    public override string CommandName => "waifuunmerge";
    public override string Description => "Развести супругов";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms => [Platform.Telegram];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "identifier",
                Description = "ID или имя хоста",
                Type = "string",
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
