using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Http;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class WaifuUnmergeCommand(IWaifuGachaClient waifuGachaClient) : BaseCommand
{
    public override string CommandName => "waifuunmerge";
    public override string Description => "Развести супруга";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms =>
        [Platform.Telegram, Platform.Api, Platform.Twitch];

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
                "Не удалось определить пользователя",
                CommandErrorCode.BadArguments
            );
        }

        var message = await waifuGachaClient.UnmergeHusbandAsync(twitchId, cancellationToken);
        if (string.IsNullOrWhiteSpace(message))
        {
            return CommandResult.Fail("Не удалось развести", CommandErrorCode.TargetUnreachable);
        }

        return CommandResult.Ok(message);
    }
}
