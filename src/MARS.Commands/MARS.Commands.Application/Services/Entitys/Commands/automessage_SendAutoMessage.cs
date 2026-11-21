using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
<<<<<<< HEAD:src/MARS.Commands/MARS.Commands.Application/Services/Entitys/Commands/automessage_SendAutoMessage.cs
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
=======
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Shared.Contracts.Alerts;
>>>>>>> origin/main:src/MARS.Commands/Services/Entitys/Commands/mikubeam_command.cs

namespace MARS.Commands.Application.Services.Entitys.Commands;

<<<<<<< HEAD:src/MARS.Commands/MARS.Commands.Application/Services/Entitys/Commands/automessage_SendAutoMessage.cs
public class AutomessageSendAutoMessageCommand : BaseCommand
{
    public override string CommandName => "automessage";
    public override string Description => "Отправить AutoMessage принудительно";
=======
public class MikubeamCommand(IAlertsCommandClient alertsCommandClient) : BaseCommand
{
    public override string CommandName => "mikubeam";

    public override string Description => "Досрочная активация MIKU MIKU BEAM эффекта";

>>>>>>> origin/main:src/MARS.Commands/Services/Entitys/Commands/mikubeam_command.cs
    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms =>
        [Platform.Api, Platform.Telegram, Platform.Twitch];

<<<<<<< HEAD:src/MARS.Commands/MARS.Commands.Application/Services/Entitys/Commands/automessage_SendAutoMessage.cs
    public override Task<CommandResult> ExecuteAsync(
=======
    public override string[] Aliases => ["mikumikubeam", "mmb"];

    public override async Task<CommandResult> ExecuteAsync(
>>>>>>> origin/main:src/MARS.Commands/Services/Entitys/Commands/mikubeam_command.cs
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
