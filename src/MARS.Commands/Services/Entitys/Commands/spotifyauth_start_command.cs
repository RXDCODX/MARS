using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class SpotifyAuthStartCommand : BaseCommand
{
    public override string CommandName => "spotifyauthstart";
    public override string Description => "Генерирует ссылку для старта авторизации Spotify";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms =>
        [Platform.Telegram, Platform.Api, Platform.Discord];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "redirectUri",
                Description = "Redirect URI для callback Spotify",
                Type = CommandParameterType.String,
                Required = true,
            },
        ];

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
