using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class MikuMondayRewardCommand : BaseCommand
{
    public override string CommandName => "mikumonday";
    public override string Description => "Ручная выдача Miku Monday: без очереди, по нику (или стример без параметра)";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api, Platform.Twitch];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "nickname",
                Description = "Никнейм пользователя Twitch. Если не указан — считается стримером",
                Type = "string",
                Required = false,
            },
        ];

    public override Task<string> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        // Stub: external dependencies not available in Commands microservice
        return Task.FromResult("Команда недоступна в текущей конфигурации (нет подключения к БД)");
    }
}
