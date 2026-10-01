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
