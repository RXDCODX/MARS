using System.Collections.Concurrent;

namespace MARS.Telegram.Services.MemoryStorageService;

/// <summary>
/// Provides in-memory storage functionality for the application.
/// </summary>
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

        var content = new MemoryFile
        {
            Extension = extension,
            FileContent = fileContent,
            FileName = fileName,
            UseCount = 1,
        };

        FileStorage.AddOrUpdate(fileName, content, (_, _) => content);

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

    public static Task<(MemoryStream description, string contentType)> GetFileStreamWithContentTypeAsync(
        string fileName
    )
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

    private sealed class MemoryFile
    {
        public required string Extension { get; init; }
        public required byte[] FileContent { get; init; }
        public required string FileName { get; init; }
        public int UseCount { get; set; }

        public string GetContentType()
        {
            return Extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                _ => "application/octet-stream",
            };
        }
    }
}
