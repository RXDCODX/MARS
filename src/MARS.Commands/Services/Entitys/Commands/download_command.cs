using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class DownloadCommand : BaseCommand
{
    public override string CommandName => "download";
    public override string Description => "Скачать трек/видео (YouTube, SoundCloud)";
    public override bool IsAdminCommand => false;
    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Discord];

    public override string[] Aliases => ["ytdownload", "dl"];

    public override CommandVisibility Visibility => CommandVisibility.All;

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "url",
                Description = "URL видео с YouTube или SoundCloud",
                Type = "string",
                Required = true,
            },
            new()
            {
                Name = "message",
                Description = "Message объект из телеграма",
                Type = "Message",
                Required = false,
            },
            new()
            {
                Name = "discord_channel_id",
                Description = "Discord channel ID",
                Type = "ulong",
                Required = false,
            },
            new()
            {
                Name = "discord_message_id",
                Description = "Discord message ID для reply",
                Type = "ulong",
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
        return Task.FromResult("Команда недоступна в текущей конфигурации (микросервис)");
    }
}
