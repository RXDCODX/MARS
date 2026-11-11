using System.Diagnostics.CodeAnalysis;
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

    private static readonly TimeSpan CacheRefreshInterval = TimeSpan.FromSeconds(20);

    private readonly string _root = Path.GetFullPath(environment.WebRootPath);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly CancellationTokenSource _stopping = new();
    private Task? _refreshTask;
    private long _version;

    public void Invalidate() => Interlocked.Increment(ref _version);

    public async Task<IReadOnlyList<DirectoryTreeNode>> GetSnapshotAsync(
        CancellationToken cancellationToken
    )
    {
        if (TryGetSnapshot(Volatile.Read(ref _version), out var cached))
        {
            return cached.Directories;
        }

        try
        {
            return await RefreshSnapshotAsync(cancellationToken, forceRefresh: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            if (memoryCache.TryGetValue(CacheKey, out CacheSnapshot? stale) && stale is not null)
            {
                logger.LogWarning(
                    exception,
                    "Could not refresh the MediaStorage directory tree; serving the previous snapshot."
                );
                return stale.Directories;
            }

            throw;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // StartAsync is awaited by the host, so the first snapshot is ready
            // before Kestrel starts accepting page requests.
            await GetSnapshotAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not warm the MediaStorage directory tree cache.");
        }

        _refreshTask = RefreshPeriodicallyAsync(_stopping.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stopping.Cancel();
        if (_refreshTask is not null)
        {
            await _refreshTask.WaitAsync(cancellationToken);
        }
    }

    private async Task RefreshPeriodicallyAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(CacheRefreshInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    await RefreshSnapshotAsync(cancellationToken, forceRefresh: true);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Periodic MediaStorage directory tree refresh failed.");
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal host shutdown.
        }
    }

    private async Task<IReadOnlyList<DirectoryTreeNode>> RefreshSnapshotAsync(
        CancellationToken cancellationToken,
        bool forceRefresh
    )
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            while (true)
            {
                var version = Volatile.Read(ref _version);
                if (!forceRefresh && TryGetSnapshot(version, out var cached))
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
                    forceRefresh = false;
                    continue;
                }

                // Keep the previous immutable snapshot available until this new
                // one is complete. The background refresh never exposes an empty cache.
                memoryCache.Set(CacheKey, new CacheSnapshot(version, directories));
                return directories;
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private bool TryGetSnapshot(long version, [NotNullWhen(true)] out CacheSnapshot? snapshot)
    {
        return memoryCache.TryGetValue(CacheKey, out snapshot)
            && snapshot is not null
            && snapshot.Version == version;
    }

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
