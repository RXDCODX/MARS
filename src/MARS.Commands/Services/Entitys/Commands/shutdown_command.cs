using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

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

    public override Task<string> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult("Команда не реализована: остановка сервиса не выполняется.");
    }
}
