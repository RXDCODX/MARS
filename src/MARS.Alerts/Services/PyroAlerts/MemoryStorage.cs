using System.Collections.Concurrent;
using MARS.Alerts.Extensions;
using MARS.Shared.Hubs.Models;
using MARS.Shared.Models.Media;

namespace MARS.Alerts.Services.PyroAlerts;

public static class MemoryStorage
{
    private static readonly ConcurrentDictionary<string, MemoryFile> FileStorage;

    static MemoryStorage()
    {
        FileStorage = [];
    }

    public static async Task<string> AddFileAsync(string fileName, byte[] fileContent)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentNullException(nameof(fileName), "Имя файла не может быть пустым");
        }

        if (FileExists(fileName))
        {
            await IncrementUseCounterAsync(fileName);
            return "/memory" + fileName;
        }

        var extension = Path.GetExtension(fileName);
        var mediaType = await extension.GetFileMediaTypeAsync();

        var content = new MemoryFile
        {
            Extension = extension,
            MediaType = mediaType,
            FileContent = fileContent,
            FileName = fileName,
            UseCount = 1,
        };

        FileStorage.AddOrUpdate(fileName, content, ((_, _) => content));

        return "/memory" + fileName;
    }

    private static async Task IncrementUseCounterAsync(string fileName)
    {
        var isFound = FileStorage.TryGetValue(fileName, out var description);

        if (!isFound || description is null)
        {
            throw new NullReferenceException();
        }

        ++description.UseCount;
        while (!FileStorage.TryUpdate(fileName, description, description))
        {
            await Task.Delay(500);
        }
    }

    public static Task<(
        MemoryStream description,
        string contentType
    )> GetFileStreamWithContentTypeAsync(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentNullException(nameof(fileName), "Имя файла не может быть пустым");
        }

        if (!FileStorage.TryGetValue(fileName, out var description))
        {
            throw new FileNotFoundException($"Файл с именем '{fileName}' не найден в хранилище");
        }

        var stream = new MemoryStream(description.FileContent);

        return Task.FromResult((stream, description.GetContentType()));
    }

    public static bool FileExists(string fileName)
    {
        return !string.IsNullOrWhiteSpace(fileName) && FileStorage.ContainsKey(fileName);
    }

    public static async Task DeleteFileAsync(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentNullException(nameof(fileName), "Имя файла не может быть пустым");
        }

        var description = FileStorage[fileName];

        --description.UseCount;
        if (description.UseCount != 0)
        {
            return;
        }

        while (true)
        {
            var isRemoved = FileStorage.TryRemove(fileName, out var removedDescription);
            if (isRemoved)
            {
                if (removedDescription != null)
                {
                    Array.Clear(removedDescription.FileContent);
                }
                break;
            }
            else
            {
                if (description.UseCount != 0)
                {
                    break;
                }
                else
                {
                    await Task.Delay(500);
                }
            }
        }
    }

    public static Task<string[]> GetAllFileNamesAsync()
    {
        var fileNames = new string[FileStorage.Count];
        FileStorage.Keys.CopyTo(fileNames, 0);
        return Task.FromResult(fileNames);
    }

    public static Task ClearStorageAsync()
    {
        FileStorage.Clear();
        return Task.CompletedTask;
    }

    public static void ClearStorage()
    {
        FileStorage.Clear();
    }

    public static int FileCount => FileStorage.Count;

    public static ulong StorageSize =>
        Convert.ToUInt64(FileStorage.Values.Sum(e => e.FileContent.Length));

    private class MemoryFile
    {
        public required string Extension { get; set; }
        public required MediaType MediaType { get; set; }
        public required byte[] FileContent { get; set; }
        public required string FileName { get; set; }
        public int UseCount { get; set; }

        public string GetContentType()
        {
            return MediaType switch
            {
                MediaType.Image => "image/" + Extension.TrimStart('.'),
                MediaType.Video => "video/" + Extension.TrimStart('.'),
                MediaType.Audio => "audio/" + Extension.TrimStart('.'),
                MediaType.Gif => "image/gif",
                _ => "application/octet-stream",
            };
        }
    }
}
