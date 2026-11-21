using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Shared.Contracts.Alerts;

namespace MARS.Commands.Services.Entitys.Commands;

public class MikubeamCommand(IAlertsCommandClient alertsCommandClient) : BaseCommand
{
    public override string CommandName => "mikubeam";

    public override string Description => "Досрочная активация MIKU MIKU BEAM эффекта";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms =>
        [Platform.Api, Platform.Telegram, Platform.Twitch];

    public override string[] Aliases => ["mikumikubeam", "mmb"];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var result = await alertsCommandClient.InvokeAsync(
            new MikubeamAlertCommandPayload(),
            cancellationToken
        );

        return CommandResult.Ok(result);
    }
}
