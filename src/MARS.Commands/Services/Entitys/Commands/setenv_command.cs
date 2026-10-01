using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

/// <summary>
/// Команда объявлена, но переменные окружения не меняет: они задаются
/// контейнеру при старте. Раньше она отвечала «Переменная окружения
/// установлена», не сделав ничего.
/// </summary>
public class SetenvCommand : BaseCommand
{
    public override string CommandName => "setenv";
    public override string Description => "Установить переменную окружения (не реализовано)";
    public override bool IsAdminCommand => true;

    public override Task<string> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(
            "Команда не реализована: переменные окружения задаются контейнеру при старте."
        );
    }
}
