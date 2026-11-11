using MARS.MediaStorage.Application.Media;
using MARS.MediaStorage.Application.Storage;
using Microsoft.AspNetCore.Hosting;

namespace MARS.MediaStorage.Infrastructure.Media;

/// <summary>
/// Безопасный одноуровневый просмотр физической структуры WebRootPath.
/// Не следует по symbolic links/reparse points и не показывает внутренние каталоги.
/// </summary>
public sealed class FileSystemMediaStorageBrowserService(
    IWebHostEnvironment environment,
    IMediaStorageService storage
) : IMediaStorageBrowserService
{
    private readonly string _root = Path.GetFullPath(environment.WebRootPath);

    public async Task<MediaStorageBrowseResult> BrowseAsync(
        string? directory,
        CancellationToken cancellationToken = default
    )
    {
        var relativeDirectory = NormalizeDirectory(directory);
        var fullDirectory = ResolveDirectory(relativeDirectory);

        if (!Directory.Exists(fullDirectory))
        {
            throw new DirectoryNotFoundException("Каталог хранилища не найден.");
        }

        EnsureNotReparsePoint(fullDirectory);

        var entries = await storage.ListDirectoryAsync(relativeDirectory, cancellationToken);
        var entryComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var indexedEntries = entries.ToDictionary(
            entry => MediaPath.Normalize(entry.Path),
            entry => entry,
            entryComparer
        );

        var directories = new List<MediaStorageBrowseDirectory>();
        foreach (
            var childPath in Directory.EnumerateDirectories(
                fullDirectory,
                "*",
                SearchOption.TopDirectoryOnly
            )
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            var name = Path.GetFileName(childPath);
            if (!IsVisibleSegment(name) || !IsWithinRoot(childPath))
            {
                continue;
            }

            if (IsReparsePoint(childPath))
            {
                continue;
            }

            var relativePath = MediaPath.Combine(relativeDirectory, name);
            directories.Add(
                new MediaStorageBrowseDirectory(
                    name,
                    relativePath,
                    HasVisibleChildDirectory(childPath)
                )
            );
        }

        var files = new List<MediaStorageBrowseFile>();
        foreach (
            var childPath in Directory.EnumerateFiles(
                fullDirectory,
                "*",
                SearchOption.TopDirectoryOnly
            )
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            var name = Path.GetFileName(childPath);
            if (!IsVisibleSegment(name) || !IsWithinRoot(childPath) || IsReparsePoint(childPath))
            {
                continue;
            }

            var file = new FileInfo(childPath);
            var relativePath = MediaPath.Combine(relativeDirectory, name);
            indexedEntries.TryGetValue(relativePath, out var entry);

            files.Add(
                new MediaStorageBrowseFile(
                    name,
                    relativePath,
                    file.Length,
                    new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero),
                    entry?.Id,
                    entry is null ? null : (int)entry.MediaType,
                    entry is not null,
                    entry?.UploadedAt,
                    entry?.LastDownloadedAt
                )
            );
        }

        var sortedDirectories = directories
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .ToArray();
        var sortedFiles = files
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .ToArray();

        var name = relativeDirectory.Length == 0 ? "wwwroot" : Path.GetFileName(relativeDirectory);
        var parent = GetParentDirectory(relativeDirectory);

        return new MediaStorageBrowseResult(
            name,
            relativeDirectory,
            parent,
            sortedDirectories,
            sortedFiles
        );
    }

    private string ResolveDirectory(string relativeDirectory)
    {
        if (!Directory.Exists(_root))
        {
            throw new DirectoryNotFoundException("Каталог хранилища не найден.");
        }

        EnsureNotReparsePoint(_root);

        var fullPath =
            relativeDirectory.Length == 0
                ? _root
                : Path.GetFullPath(
                    Path.Combine(_root, relativeDirectory.Replace('/', Path.DirectorySeparatorChar))
                );

        if (!IsWithinRoot(fullPath))
        {
            throw new ArgumentException("Недопустимый путь каталога.");
        }

        var current = _root;
        foreach (var segment in relativeDirectory.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (!Directory.Exists(current))
            {
                throw new DirectoryNotFoundException("Каталог хранилища не найден.");
            }

            EnsureNotReparsePoint(current);
        }

        return fullPath;
    }

    private bool IsWithinRoot(string fullPath)
    {
        var full = Path.GetFullPath(fullPath);
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var rootPrefix = Path.EndsInDirectorySeparator(_root)
            ? _root
            : _root + Path.DirectorySeparatorChar;

        return string.Equals(full, _root, comparison) || full.StartsWith(rootPrefix, comparison);
    }

    private static string NormalizeDirectory(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return string.Empty;
        }

        var raw = directory.Trim();
        if (
            raw.IndexOf('\0') >= 0
            || Path.IsPathRooted(raw)
            || raw.StartsWith("/", StringComparison.Ordinal)
            || raw.StartsWith('\\')
            || (raw.Length >= 2 && raw[1] == ':')
        )
        {
            throw new ArgumentException("Недопустимый путь каталога.");
        }

        var segments = raw.Replace('\\', '/').Split('/');
        if (
            segments.Any(segment =>
                segment.Length == 0 || segment is "." or ".." || !IsVisibleSegment(segment)
            )
        )
        {
            throw new ArgumentException("Недопустимый путь каталога.");
        }

        return string.Join('/', segments);
    }

    private static string? GetParentDirectory(string relativeDirectory)
    {
        var separatorIndex = relativeDirectory.LastIndexOf('/');
        return separatorIndex < 0 ? null : relativeDirectory[..separatorIndex];
    }

    private static bool IsVisibleSegment(string name) =>
        name.Length > 0
        && !name.StartsWith(".", StringComparison.Ordinal)
        && !name.Equals(".git", StringComparison.OrdinalIgnoreCase)
        && !name.Equals("_trash", StringComparison.OrdinalIgnoreCase);

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch (FileNotFoundException)
        {
            return true;
        }
        catch (DirectoryNotFoundException)
        {
            return true;
        }
    }

    private static void EnsureNotReparsePoint(string path)
    {
        if (IsReparsePoint(path))
        {
            throw new UnauthorizedAccessException(
                "Символические ссылки в хранилище не поддерживаются."
            );
        }
    }

    private static bool HasVisibleChildDirectory(string directory)
    {
        foreach (
            var child in Directory.EnumerateDirectories(
                directory,
                "*",
                SearchOption.TopDirectoryOnly
            )
        )
        {
            var name = Path.GetFileName(child);
            if (IsVisibleSegment(name) && !IsReparsePoint(child))
            {
                return true;
            }
        }

        return false;
    }
}
