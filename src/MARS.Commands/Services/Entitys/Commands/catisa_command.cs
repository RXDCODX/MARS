using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class CatisaCommand : BaseCommand
{
    public override string CommandName => "catisa";
    public override string Description => "Отправить сообщение на твич канал";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms => [Platform.Api, Platform.Telegram];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "channel",
                Description = "Канал для отправки сообщения",
                Type = "string",
                Required = true,
            },
            new()
            {
                Name = "text",
                Description = "Текст для отправки сообщения",
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
