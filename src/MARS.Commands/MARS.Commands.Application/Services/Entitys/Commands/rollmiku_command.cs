using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Clients.Http;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class RollMikuCommand(IWaifuGachaClient waifuGachaClient) : BaseCommand
{
    public override string CommandName => "rollmiku";
    public override string Description => "Выполняет ролл костюма Miku";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms =>
        [Platform.Telegram, Platform.Api, Platform.Twitch];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var message = await waifuGachaClient.RollMikuAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(message))
        {
            return CommandResult.Fail(
                "Не удалось выполнить Miku roll",
                CommandErrorCode.TargetUnreachable
            );
        }

        return CommandResult.Ok(message);
    }
}
