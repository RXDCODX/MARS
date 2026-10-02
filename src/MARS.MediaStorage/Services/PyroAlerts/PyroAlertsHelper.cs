using MARS.MediaStorage.Entities;
using MARS.MediaStorage.Extensions;
using MARS.MediaStorage.Services.MemoryStorageService;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MARS.MediaStorage.Services.PyroAlerts;

public class PyroAlertsHelper(ILogger<PyroAlertsHelper> logger)
{
    public async Task<MediaInfo?> GetTransferObj(ITelegramBotClient client, Message message)
    {
        try
        {
            var fileInfo = await GetTgFileInfo(client, message);

            if (fileInfo == null)
            {
                return null;
            }

            var fileContent = await DownloadFile(client, fileInfo);

            var downloadPath = fileInfo.FilePath ?? throw new NullReferenceException();
            var extension = Path.GetExtension(downloadPath);
            var fileType = await extension.GetFileMediaTypeAsync();

            var mediainfo = new MediaInfo
            {
                FileInfo = new MediaFileInfo
                {
                    Extension = extension,
                    Type = fileType,
                    FileName = fileInfo.FilePath,
                    IsLocalFile = true,
                    FilePath = "memory/" + fileInfo.FilePath,
                },
                MetaInfo = new MediaMetaInfo
                {
                    DisplayName = message.Chat.Username ?? string.Empty,
                    IsLooped = fileType == MediaType.Video,
                    Vip = false,
                },
                PositionInfo = new MediaPositionInfo
                {
                    IsRotated = true,
                    IsResizeRequires = true,
                    Height = 500,
                    Width = 500,
                },
                TextInfo = new MediaTextInfo(),
                StylesInfo = new MediaStylesInfo(),
            };

            switch (fileType)
            {
                case MediaType.Video:
                    mediainfo.StylesInfo.IsBorder = true;
                    mediainfo.MetaInfo.IsLooped = true;
                    mediainfo.PositionInfo.Height = 500;
                    mediainfo.PositionInfo.Width = 500;
                    mediainfo.PositionInfo.IsResizeRequires = true;
                    break;
                case MediaType.None:
                    return null;
                case MediaType.Audio:
                    mediainfo.MetaInfo.IsLooped = false;
                    break;
                case MediaType.TelegramSticker:
                    mediainfo.PositionInfo.IsProportion = true;
                    mediainfo.PositionInfo.IsResizeRequires = true;
                    mediainfo.PositionInfo.Height = 600;
                    mediainfo.PositionInfo.Width = 600;
                    break;
            }

            await MemoryStorage.AddFileAsync(fileInfo.FilePath, fileContent);

            return mediainfo;
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
        }

        return null;
    }

    public async Task<byte[]> DownloadFile(ITelegramBotClient client, TgFileInfo fileInfo)
    {
        if (!fileInfo.FileSize.HasValue)
        {
            throw new NullReferenceException();
        }

        var buffer = new byte[fileInfo.FileSize.Value];
        await using var stream = new MemoryStream(buffer, true);
        try
        {
            if (fileInfo.FilePath != null)
            {
                await client.DownloadFile(fileInfo.FilePath, stream);
            }
            else
            {
                Array.Clear(buffer);
            }
        }
        catch (Exception)
        {
            Array.Clear(buffer);
        }

        return buffer;
    }

    public async Task DownloadFileAndCache(
        ITelegramBotClient client,
        TgFileInfo fileInfo,
        string folderPath
    )
    {
        var filePath = Path.Combine(
            folderPath,
            fileInfo.FilePath ?? throw new InvalidOperationException()
        );

        EnsureDirectoryExists(filePath);

        await using var stream = new FileStream(filePath, FileMode.Create);
        if (fileInfo.FilePath != null)
        {
            await client.DownloadFile(fileInfo.FilePath, stream);
        }
    }

    public void EnsureDirectoryExists(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var directoryPath = Path.GetDirectoryName(filePath);
        var directories = directoryPath!.Split(Path.DirectorySeparatorChar);

        var currentPath = directories[0];
        if (directories != null)
        {
            for (var i = 1; i < directories.Length; i++)
            {
                currentPath += Path.DirectorySeparatorChar + directories[i];
                if (!Directory.Exists(currentPath))
                {
                    Directory.CreateDirectory(currentPath);
                }
            }
        }
    }

    public async Task<TgFileInfo?> GetChatPhotoFilePath(
        ITelegramBotClient client,
        ChatFullInfo chat
    )
    {
        if (chat.Photo == null)
        {
            return null;
        }

        var file = await client.GetFile(chat.Photo.BigFileId);
        return new TgFileInfo { FilePath = file.FilePath, FileSize = file.FileSize };
    }

    public async Task<TgFileInfo?> GetTgFileInfo(ITelegramBotClient client, Message? message)
    {
        try
        {
            if (message != null)
            {
                string? fileId =
                    message.Photo != null ? message.Photo.LastOrDefault()!.FileId
                    : message.Video != null ? message.Video.FileId
                    : message.Voice != null ? message.Voice.FileId
                    : message.Sticker != null ? message.Sticker.FileId
                    : message.Animation != null ? message.Animation.FileId
                    : message.Document != null ? message.Document.FileId
                    : message.Audio != null ? message.Audio.FileId
                    : null;

                if (fileId != null)
                {
                    var file = await client.GetFile(fileId);
                    return new TgFileInfo { FilePath = file.FilePath, FileSize = file.FileSize };
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }

        return null;
    }
}
