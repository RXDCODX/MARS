using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class ShutdownCommand : BaseCommand
{
    public override string CommandName => "shutdown";
    public override string Description => "Выключение сервиса";
    public override bool IsAdminCommand => true;

    public override Task<string> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        // Простое сообщение, реальное завершение выполняется в другом месте
        return Task.FromResult("Сервер будет остановлен");
    }
}
