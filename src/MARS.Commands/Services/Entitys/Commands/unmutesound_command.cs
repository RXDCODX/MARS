using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.Shared.Clients;

namespace MARS.Commands.Services.Entitys.Commands;

public class unmutesound_command(IAudioControllerClient audioControllerClient) : BaseCommand
{
    public override string CommandName => "unmutesound";
    public override string Description => "Включить звук на компухтере";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var result = await audioControllerClient.UnmuteAsync(cancellationToken: cancellationToken);

        if (result is null)
        {
            return CommandResult.Fail(
                "Комплюхтер не удалось размутить: аудиоконтроллер недоступен.",
                CommandErrorCode.TargetUnreachable
            );
        }

        return CommandResult.Ok(
            $"Комплюхтер размучен успешно. Разблокировано сессий: {result.UnmutedSessionCount}."
        );
    }
}
