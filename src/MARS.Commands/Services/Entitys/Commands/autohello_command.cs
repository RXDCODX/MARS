using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.Shared.Clients;

namespace MARS.Commands.Services.Entitys.Commands;

/// <summary>
/// Включение и выключение приветствия от супруга. Состояние хранится в
/// <c>MARS.WaifuGacha</c>, поэтому команда ходит туда по внутреннему API.
/// </summary>
public class AutoHelloCommand(IWaifuGachaClient waifuGachaClient) : BaseCommand
{
    public override string CommandName => "autohello";
    public override string Description => "Включить или выключить приветствие от супруга(и)";
    public override bool IsAdminCommand => false;
    public override Platform[] AvailablePlatforms => [Platform.Twitch];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var twitchId = TwitchCaller.ResolveId(parameters);

        if (string.IsNullOrWhiteSpace(twitchId))
        {
            return CommandResult.Fail(
                "Не удалось определить вашего пользователя.",
                CommandErrorCode.BadArguments
            );
        }

        var enabled = await waifuGachaClient.ToggleAutoHelloAsync(twitchId, cancellationToken);

        return CommandResult.Ok(
            enabled ? "Приветствие от супруга(и) включено." : "Приветствие от супруга(и) выключено."
        );
    }
}
