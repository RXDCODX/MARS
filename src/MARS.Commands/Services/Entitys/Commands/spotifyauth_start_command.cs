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
    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api, Platform.Discord];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "redirectUri",
                Description = "Redirect URI для callback Spotify",
                Type = "string",
                Required = true,
            },
        ];

    public override Task<string> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        // Stub: external dependencies not available in Commands microservice
        return Task.FromResult("Команда недоступна в текущей конфигурации (микросервис)");
    }
}
