using MARS.MediaStorage.Entities;
using MARS.MediaStorage.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MARS.Shared.Models.Media;

namespace MARS.MediaStorage.Services.Media;

public class WebRootMediaFileStorageService(
    IWebHostEnvironment env,
    ILogger<WebRootMediaFileStorageService> logger
) : IMediaFileStorageService
{
    private const string DefaultFolderName = "Alerts/uploaded_mems";

    /// <summary>
    /// Аудит: <c>DeleteFileAsync</c> и <c>CopyToDevCopiesAsync</c> склеивали путь
    /// без всякой проверки. Значение <c>FilePath</c> берётся из БД, поэтому
    /// «../../etc/passwd» приводил к чтению/удалению файла за пределами
    /// хранилища. Резолв теперь единственный и отбрасывает выход за корень.
    /// </summary>
    private string ResolveFullPath(string relativePath)
    {
        // Пути хранилища в БД/URL имеют вид «/Alerts/x.mp4», а MediaPath отдаёт
        // форму без ведущего слеша. Нормализуем до проверки, иначе легитимный
        // путь отсекался бы как абсолютный.
        var normalized = MediaPath.Normalize(relativePath);

        if (!MediaPath.IsSafeRelative(normalized))
        {
            throw new InvalidOperationException(
                $"Недопустимый относительный путь: '{relativePath}'"
            );
        }

        return Path.GetFullPath(
            Path.Combine(env.WebRootPath, normalized.Replace('/', Path.DirectorySeparatorChar))
        );
    }

    public async Task<MediaFileInfo> SaveFileAsync(
        IFormFile file,
        string? targetRelativePathHint = null
    )
    {
        var extension = Path.GetExtension(file.FileName) ?? string.Empty;
        var relativePath = ResolveRelativePath(targetRelativePathHint, extension);
        var fullPath = ResolveFullPath(relativePath);

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using (var fs = File.Create(fullPath))
        {
            await file.CopyToAsync(fs);
            await fs.FlushAsync();
        }

        var relativeUrl = NormalizePath(relativePath);

        var mediaType = await extension.GetFileMediaTypeAsync();

        var info = new MediaFileInfo
        {
            Type = mediaType,
            FilePath = relativeUrl,
            IsLocalFile = true,
            FileName = MediaPath.GetFileName(fullPath),
            Extension = extension,
        };

        try
        {
            await CopyToDevCopiesAsync(info.FilePath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to copy media file to dev copies");
        }

        return info;
    }

    public Task DeleteFileAsync(string relativePath)
    {
        var full = ResolveFullPath(relativePath);
        if (File.Exists(full))
        {
            File.Delete(full);
        }

        return Task.CompletedTask;
    }

    public Task CopyToDevCopiesAsync(string relativePath)
    {
        var sourceFull = ResolveFullPath(relativePath);

        if (!File.Exists(sourceFull))
        {
            return Task.CompletedTask;
        }

        var devWebRoot = ResolveDevWebRoot();
        var destFull = Path.Combine(
            devWebRoot,
            MediaPath.Normalize(relativePath).Replace('/', Path.DirectorySeparatorChar)
        );

        var destDirectory = Path.GetDirectoryName(destFull);
        if (!string.IsNullOrWhiteSpace(destDirectory))
        {
            Directory.CreateDirectory(destDirectory);
        }

        File.Copy(sourceFull, destFull, true);

        return Task.CompletedTask;
    }

    private static string ResolveRelativePath(
        string? targetRelativePathHint,
        string sourceExtension
    )
    {
        if (string.IsNullOrWhiteSpace(targetRelativePathHint))
        {
            var defaultFileName = $"{Guid.NewGuid()}{sourceExtension}";

            return "/" + MediaPath.Combine(DefaultFolderName, defaultFileName);
        }

        // Аудит: проверка была `Path.IsPathRooted(trimmed) || trimmed.Contains("..")`.
        // На Linux IsPathRooted("C:\\evil") == false, а Contains("..") отсекал
        // безобидные имена вроде "мем..mp4". Проверка вынесена в MediaPath.
        if (!MediaPath.IsSafeRelative(targetRelativePathHint))
        {
            throw new InvalidOperationException("Некорректный относительный путь для файла");
        }

        var normalizedHint = MediaPath.Normalize(targetRelativePathHint);
        var finalPath = normalizedHint;

        if (string.IsNullOrWhiteSpace(Path.GetExtension(normalizedHint)))
        {
            finalPath = MediaPath.Combine(normalizedHint, sourceExtension.TrimStart('.'));
        }

        return "/" + finalPath;
    }

    private string ResolveDevWebRoot()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var projectRoot = FindProjectRoot(currentDirectory);

        if (!string.IsNullOrWhiteSpace(projectRoot))
        {
            var candidate = Path.Combine(projectRoot, "wwwroot");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return env.WebRootPath;
    }

    private static string? FindProjectRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);

        while (dir != null)
        {
            if (dir.GetFiles("*.csproj").Length > 0)
            {
                return dir.FullName;
            }

            if (dir.Parent == null)
            {
                break;
            }

            dir = dir.Parent;
        }

        return null;
    }

    /// <summary>
    /// URL-форма пути хранилища: прямые слеши и обязательный ведущий «/»
    /// (именно в таком виде FilePath отдаётся в БД и в API).
    /// Файловая форма без ведущего слеша — <see cref="MediaPath.Normalize"/>.
    /// </summary>
    private static string NormalizePath(string? path)
    {
        var normalized = MediaPath.Normalize(path);

        return normalized.Length == 0 ? string.Empty : "/" + normalized;
    }
}
