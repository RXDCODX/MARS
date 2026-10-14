using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.Shared.Clients;

namespace MARS.Commands.Services.Entitys.Commands;

public class mutesound_command(IAudioControllerClient audioControllerClient) : BaseCommand
{
    public override string CommandName => "mutesound";
    public override string Description => "Выключить звук на компухтере";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var result = await audioControllerClient.MuteAsync(cancellationToken: cancellationToken);

        if (result is null)
        {
            return CommandResult.Fail(
                "Комплюхтер не удалось замутить: аудиоконтроллер недоступен.",
                CommandErrorCode.TargetUnreachable
            );
        }

        return CommandResult.Ok(
            $"Комплюхтер замучен успешно. Заглушено сессий: {result.MutedSessionCount}."
        );
    }
}
