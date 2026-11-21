using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class TtsStopCommand : BaseCommand
{
    public override string CommandName => "ttsstop";
    public override string Description => "Остановить воспроизведение TTS";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api];

    public override Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(
            CommandResult.Fail(
                "Команда ещё не реализована: нужные сервисы не подключены.",
                CommandErrorCode.NotImplemented
            )
        );
    }
}
