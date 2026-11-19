using System.Text.Json;
using Google.Protobuf;
using MARS.Alerts.Application.Twitch.Rewards.Models;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.AspNetCore.Hosting;

namespace MARS.Alerts.Application.Twitch.Rewards;

public class HighlitedMessage(
    IWebHostEnvironment environment,
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster
)
{
    private readonly EventBroadcaster<TelegramusEvent> _broadcaster = broadcaster;

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

                        await _broadcaster.BroadcastAsync(
                            new TelegramusEvent
                            {
                                Highlite = new HighliteEvent
                                {
                                    MessageJson = ByteString.CopyFromUtf8(
                                        JsonSerializer.Serialize(messageText)
                                    ),
                                    Color = color,
                                    FaceUrlJson = image is not null
                                        ? ByteString.CopyFromUtf8(JsonSerializer.Serialize(image))
                                        : ByteString.Empty,
                                },
                            }
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
