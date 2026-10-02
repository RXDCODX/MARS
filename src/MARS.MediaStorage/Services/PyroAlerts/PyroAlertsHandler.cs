using MARS.MediaStorage.Entities;
using MARS.MediaStorage.Services.MemoryStorageService;
using MARS.Shared.Models.Media;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MARS.MediaStorage.Services.PyroAlerts;

public class PyroAlertsHandler(PyroAlertsHelper alertsHelper, IAlertNotifier alertNotifier)
{
    public async Task HandAlert(ITelegramBotClient client, Update update)
    {
        var message = update.Message;
        if (update.Type == UpdateType.Message)
        {
            switch (message!.Type)
            {
                case MessageType.Sticker:
                {
                    var mediaInfo = await alertsHelper.GetTransferObj(client, message);
                    if (mediaInfo != null)
                    {
                        await alertNotifier.SendAlertAsync(
                            new MediaDto(mediaInfo) { MediaInfo = mediaInfo }
                        );
                    }

                    break;
                }
                case MessageType.Video:
                    goto case MessageType.Sticker;
                case MessageType.Audio:
                    goto case MessageType.Sticker;
                case MessageType.Photo:
                    goto case MessageType.Sticker;
                case MessageType.Animation:
                    goto case MessageType.Sticker;
                case MessageType.Document:
                    goto case MessageType.Sticker;
                case MessageType.Voice:
                {
                    var chat = await client.GetChat(message.Chat);
                    if (chat.Photo != null)
                    {
                        var fileInfo = await alertsHelper.GetChatPhotoFilePath(client, chat);
                        if (fileInfo is { FilePath: not null })
                        {
                            if (!MemoryStorage.FileExists(fileInfo.FilePath))
                            {
                                var content = await alertsHelper.DownloadFile(client, fileInfo);
                                await MemoryStorage.AddFileAsync(fileInfo.FilePath, content);
                            }
                            else
                            {
                                await MemoryStorage.AddFileAsync(fileInfo.FilePath, []);
                            }

                            var mediaInfo = await alertsHelper.GetTransferObj(client, message);
                            if (mediaInfo != null)
                            {
                                mediaInfo.FileInfo.Type = MediaType.Voice;
                                mediaInfo.TextInfo.Text = "memory/" + fileInfo.FilePath;
                                mediaInfo.MetaInfo.DisplayName = chat.Username ?? string.Empty;
                                mediaInfo.MetaInfo.Priority = MediaAlertPriority.High;
                                mediaInfo.FileInfo.IsLocalFile = true;

                                await alertNotifier.SendAlertAsync(
                                    new MediaDto { MediaInfo = mediaInfo }
                                );
                                break;
                            }
                        }
                    }

                    goto case MessageType.Sticker;
                }
                case MessageType.Text:
                    goto case MessageType.Sticker;
            }
        }
    }
}
