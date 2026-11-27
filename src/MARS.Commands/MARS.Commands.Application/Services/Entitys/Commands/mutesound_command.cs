using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Http;

namespace MARS.Commands.Application.Services.Entitys.Commands;

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
        try
        {
            var result = await audioControllerClient.MuteAsync(
                cancellationToken: cancellationToken
            );

            return CommandResult.Ok(
                $"Комплюхтер замучен успешно. Заглушено сессий: {result.MutedSessionCount}."
            );
        }
        catch (AudioControllerUnavailableException)
        {
            return CommandResult.Fail(
                "Комплюхтер не удалось замутить: аудиоконтроллер недоступен.",
                CommandErrorCode.TargetUnreachable
            );
        }
    }
}
