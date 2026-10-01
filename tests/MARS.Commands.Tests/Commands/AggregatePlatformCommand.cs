using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Tests.Commands;

/// <summary>
/// Команда, объявленная под агрегат платформ, а не под одну. Нужна, чтобы проверить,
/// что <c>IsAvailableOnPlatform</c> умеет <c>[Flags]</c>: до правки сравнение шло
/// через <c>Enumerable.Contains</c> и агрегат <c>Twitch | Discord</c> не совпадал
/// ни с одним отдельным значением.
/// </summary>
public sealed class AggregatePlatformCommand : BaseCommand
{
    public override string CommandName => "aggregateplatform";

    public override string Description => "Проверка агрегата платформ";

    public override bool IsAdminCommand => false;

    public override Platform[] AvailablePlatforms => [Platform.Twitch | Platform.Discord];

    public override Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(CommandResult.Ok("ok"));
    }
}
