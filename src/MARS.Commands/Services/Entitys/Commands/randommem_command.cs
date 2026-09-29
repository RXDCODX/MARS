using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class RandomMemCommand : BaseCommand
{
    public override string CommandName => "randommem";
    public override string Description => "Включает или выключает онлайн-режим рандомных мемов";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms => [Platform.Api, Platform.Telegram, Platform.Twitch];

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
