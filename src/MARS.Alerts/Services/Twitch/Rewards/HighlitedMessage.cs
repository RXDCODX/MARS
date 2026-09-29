using MARS.Shared.Hubs;
using MARS.Shared.Hubs.Interfaces;
using MARS.Alerts.Models;
using MARS.Shared.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Alerts.Services.Twitch.Rewards;

public class HighlitedMessage(
    IHubContext<TelegramusHub, ITelegramusHub> hubContext,
    IWebHostEnvironment environment,
    RickRollerService rickRollerService
)
{
    public async Task HandleHighlightedMessage(
        TwitchUser user,
        string messageText,
        string hexColor,
        bool isVip,
        bool isModerator,
        bool isBroadcaster
    )
    {
        if ((isVip || isModerator || isBroadcaster))
        {
            await Task.Factory.StartNew(async () =>
            {
                await rickRollerService.TryRickRollAsync(
                    user,
                    async () =>
                    {
                        var color = string.IsNullOrWhiteSpace(hexColor) ? "#ffffff" : hexColor;
                        var path = Path.Combine(
                            environment.WebRootPath ?? environment.ContentRootPath,
                            "faces"
                        );
                        AutoArtImage? image = null;

                        if (Directory.Exists(path))
                        {
                            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                            if (files.Length > 0)
                            {
                                var randomFile = files[Random.Shared.Next(files.Length)];
                                image = GetImageByFilePath(randomFile);
                            }
                        }

                        await hubContext.Clients.All.Highlite(
                            messageText,
                            color,
                            image ?? new AutoArtImage()
                        );
                    }
                );
            });
        }
    }

    private static AutoArtImage GetImageByFilePath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new NullReferenceException();
        }

        var extension = Path.GetExtension(filePath);
        filePath = filePath.Substring(
            filePath.IndexOf("wwwroot", StringComparison.Ordinal) + "wwwroot".Length
        );

        return new AutoArtImage { URL = filePath, Extension = extension };
    }
}
