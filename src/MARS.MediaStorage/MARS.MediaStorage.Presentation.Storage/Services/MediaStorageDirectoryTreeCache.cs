using MARS.MediaStorage.Application.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.MediaStorage.Presentation.Storage.Services;

/// <summary>
/// Builds and keeps the complete directory hierarchy in process memory.
/// The initial snapshot is warmed before the host starts accepting requests.
/// </summary>
public sealed class MediaStorageDirectoryTreeCache(
    IMemoryCache memoryCache,
    IWebHostEnvironment environment,
    ILogger<MediaStorageDirectoryTreeCache> logger
) : IHostedService
{
    private const string CacheKey = "MediaStorage.Presentation.DirectoryTree";
    private const int MaximumTreeItems = 500;
    private const int MaximumTreeDepth = 16;

    private static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(20);

    private readonly string _root = Path.GetFullPath(environment.WebRootPath);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private long _version;

    public void Invalidate()
    {
        Interlocked.Increment(ref _version);
        memoryCache.Remove(CacheKey);
    }

    public async Task<IReadOnlyList<DirectoryTreeNode>> GetSnapshotAsync(
        CancellationToken cancellationToken
    )
    {
        while (true)
        {
            var version = Volatile.Read(ref _version);
            if (
                memoryCache.TryGetValue(CacheKey, out CacheSnapshot? cached)
                && cached is not null
                && cached.Version == version
            )
            {
                return cached.Directories;
            }

            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                version = Volatile.Read(ref _version);
                if (
                    memoryCache.TryGetValue(CacheKey, out cached)
                    && cached is not null
                    && cached.Version == version
                )
                {
                    return cached.Directories;
                }

                var directories = await Task.Run(
                    () => BuildSnapshot(cancellationToken),
                    cancellationToken
                );

                // A write may have invalidated the cache while the filesystem
                // scan was running. Never publish that older snapshot.
                if (version != Volatile.Read(ref _version))
                {
                    continue;
                }

                memoryCache.Set(
                    CacheKey,
                    new CacheSnapshot(version, directories),
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = CacheLifetime,
                    }
                );
                return directories;
            }
            finally
            {
                _refreshLock.Release();
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await GetSnapshotAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Keep the service available: the first page request will retry the warm-up.
            logger.LogWarning(exception, "Could not warm the MediaStorage directory tree cache.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private IReadOnlyList<DirectoryTreeNode> BuildSnapshot(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_root) || IsReparsePoint(_root))
        {
            return [];
        }

        var remaining = MaximumTreeItems;
        return ReadChildren(_root, string.Empty, 0, ref remaining, cancellationToken);
    }

    private IReadOnlyList<DirectoryTreeNode> ReadChildren(
        string fullDirectory,
        string relativeDirectory,
        int depth,
        ref int remaining,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (depth >= MaximumTreeDepth || remaining <= 0)
        {
            return [];
        }

        string[] paths;
        try
        {
            paths = Directory
                .EnumerateDirectories(fullDirectory, "*", SearchOption.TopDirectoryOnly)
                .ToArray();
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogDebug(exception, "Cannot enumerate MediaStorage directory {Directory}.", relativeDirectory);
            return [];
        }
        catch (IOException exception)
        {
            logger.LogDebug(exception, "Cannot enumerate MediaStorage directory {Directory}.", relativeDirectory);
            return [];
        }

        var directories = paths
            .Select(path => new { FullPath = path, Name = Path.GetFileName(path) })
            .Where(directory =>
                MediaPath.IsVisibleSegment(directory.Name)
                && IsWithinRoot(directory.FullPath)
                && !IsReparsePoint(directory.FullPath)
            )
            .OrderBy(directory => directory.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(directory => directory.Name, StringComparer.Ordinal)
            .ToArray();

        var result = new List<DirectoryTreeNode>(Math.Min(directories.Length, remaining));
        foreach (var directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (remaining <= 0)
            {
                break;
            }

            remaining--;
            var relativePath = MediaPath.Combine(relativeDirectory, directory.Name);
            IReadOnlyList<DirectoryTreeNode> children =
                depth + 1 < MaximumTreeDepth && remaining > 0
                    ? ReadChildren(directory.FullPath, relativePath, depth + 1, ref remaining, cancellationToken)
                    : Array.Empty<DirectoryTreeNode>();

            result.Add(new DirectoryTreeNode(directory.Name, relativePath, children));
        }

        return result;
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
        catch (UnauthorizedAccessException)
        {
            return true;
        }
        catch (IOException)
        {
            return true;
        }
    }

    private sealed record CacheSnapshot(
        long Version,
        IReadOnlyList<DirectoryTreeNode> Directories
    );

    public sealed record DirectoryTreeNode(
        string Name,
        string RelativePath,
        IReadOnlyList<DirectoryTreeNode> Children
    );
}
