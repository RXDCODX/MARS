using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;

namespace MARS.Commands.Application.Services.Entitys.Commands;

/// <summary>
/// Команда объявлена, но остановку не выполняет: завершение процесса —
/// не то, что можно сделать из команды, и раньше она отвечала «Сервер
/// будет остановлен», создавая видимость работы. Пока реализации нет,
/// сообщение должно быть правдой.
/// </summary>
public class ShutdownCommand : BaseCommand
{
    public override string CommandName => "shutdown";
    public override string Description => "Выключение сервиса (не реализовано)";
    public override bool IsAdminCommand => true;

    public override Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(
            CommandResult.Ok("Команда не реализована: остановка сервиса не выполняется.")
        );
    }
}
