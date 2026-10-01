using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Tests.Commands;

/// <summary>
/// Команда с параметром типа <c>Bool</c>. Ни одна из 67 команд такой параметр не
/// объявляет — типы в проекте только <c>String</c>, <c>Int</c> и <c>Long</c>, —
/// поэтому проверять разбор <c>bool</c> больше не на чем.
/// </summary>
public sealed class BoolTypedCommandProbe : BaseCommand
{
    public override string CommandName => "booltypedprobe";

    public override string Description => "Проверка разбора типа bool";

    public override bool IsAdminCommand => false;

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "enabled",
                Description = "Флаг",
                Type = CommandParameterType.Bool,
                Required = true,
            },
        ];

    public override Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(CommandResult.Ok("ok"));
    }
}
