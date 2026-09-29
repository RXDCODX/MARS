using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class PlatformTestCommand : BaseCommand
{
    public override string CommandName => "platformtest";
    public override string Description => "Тестовая команда для демонстрации работы с платформами";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms => [Platform.All];

    public override string[] Aliases => ["ptest"];

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
