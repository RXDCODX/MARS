using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Http;

namespace MARS.Commands.Application.Services.Entitys.Commands;

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
        try
        {
            var count = await audioControllerClient.UnmuteAsync(cancellationToken);

            return CommandResult.Ok(
                $"Комплюхтер размучен успешно. Разблокировано сессий: {count}."
            );
        }
        catch (AudioControllerUnavailableException)
        {
            return CommandResult.Fail(
                "Комплюхтер не удалось размутить: аудиоконтроллер недоступен.",
                CommandErrorCode.TargetUnreachable
            );
        }
    }
}
