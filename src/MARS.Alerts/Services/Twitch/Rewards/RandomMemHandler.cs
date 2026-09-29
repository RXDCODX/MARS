using MARS.Alerts.Extensions;
using MARS.Shared.Hubs;
using MARS.Shared.Hubs.Interfaces;
using MARS.Shared.Hubs.Models;
using MARS.Alerts.Models;
using MARS.Alerts.Services.PyroAlerts;
using MARS.Shared.Models.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MARS.Alerts.Services.Twitch.Rewards;

public class RandomMemHandler(
    IWebHostEnvironment environment,
    PyroAlertsHelper helper,
    ILogger<RandomMemHandler> logger
)
{
    public bool IsServiceActive { get; set; } = true;

    public readonly string[] AlertsPaths =
    [
        Path.Combine(
            environment.WebRootPath ?? environment.ContentRootPath,
            "Alerts",
            "random_meme"
        ),
    ];

    private string? LastMediaGroupId { get; set; }

    public Task HandMessage(ITelegramBotClient client, Update update)
    {
        if (update.Type == UpdateType.Message && IsServiceActive)
        {
            var message = update.Message!;

            if (message.MediaGroupId == null)
            {
                return Task.Factory.StartNew(async () => await Process(client, message));
            }
            else
            {
                if (LastMediaGroupId == null || LastMediaGroupId != message.MediaGroupId)
                {
                    LastMediaGroupId = message.MediaGroupId;
                }

                if (LastMediaGroupId == message.MediaGroupId)
                {
                    return Task.Factory.StartNew(async () => await Process(client, message));
                }
            }
        }
        return Task.CompletedTask;
    }

    private async Task Process(ITelegramBotClient client, Message message)
    {
        var fileInfo = await helper.GetTgFileInfo(client, message);

        if (fileInfo == null)
        {
            return;
        }

        foreach (var alertsPath in AlertsPaths)
        {
            var folderPath = alertsPath;
            var downloadPath = folderPath + "\\" + fileInfo.FilePath;

            MediaType type = await Path.GetExtension(fileInfo.FilePath).GetFileMediaTypeAsync();

            switch (type)
            {
                case MediaType.Video:
                case MediaType.Image:
                    if (!File.Exists(downloadPath))
                    {
                        await helper.DownloadFileAndCache(client, fileInfo, folderPath);
                        logger.LogInformation("Downloaded file: {FilePath}", fileInfo.FilePath);
                    }
                    else
                    {
                        File.SetLastAccessTime(downloadPath, DateTime.Now);
                        logger.LogInformation(
                            "File already exists, updated access time: {FilePath}",
                            fileInfo.FilePath
                        );
                    }
                    break;
                default:
                    logger.LogWarning("Unsupported file type: {Type}", type);
                    break;
            }
        }
    }
}
