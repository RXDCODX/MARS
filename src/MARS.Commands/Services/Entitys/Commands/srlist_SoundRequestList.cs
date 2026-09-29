using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class SrlistSoundRequestListCommand : BaseCommand
{
    public override string CommandName => "srlist";
    public override string Description => "Добавить плейлист в очередь звуковых запросов с опциональным лимитом треков (только для VIP/MOD)";
    public override bool IsAdminCommand => false;
    public override Platform[] AvailablePlatforms => [Platform.Twitch];

    public override string[] Aliases => ["srlists"];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "tracksCount",
                Description =
                    "Сколько треков добавить из плейлиста. 0 или меньше - добавить максимум",
                Type = "int",
                Required = true,
                DefaultValue = "10",
            },
            new()
            {
                Name = "playlistQuery",
                Description = "URL плейлиста YouTube или SoundCloud",
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
