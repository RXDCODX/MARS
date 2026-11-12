using System.Diagnostics.CodeAnalysis;
using MARS.MediaStorage.Application.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.MediaStorage.Presentation.Storage.Services;

/// <summary>
/// Builds and keeps the complete visible file hierarchy in process memory.
/// The initial snapshot is warmed before the host starts accepting requests.
/// </summary>
public sealed class MediaStorageDirectoryTreeCache(
    IMemoryCache memoryCache,
    IWebHostEnvironment environment,
    ILogger<MediaStorageDirectoryTreeCache> logger
) : IHostedService
{
    private const string CacheKey = "MediaStorage.Presentation.DirectoryTree";

    // Directories and files have separate budgets: a folder with many files must
    // never consume the allowance of its siblings, otherwise the tree loses
    // whole branches. Directories are also traversed breadth-first so shallow
    // folders are always present before deep descendants.
    private const int MaximumDirectoryNodes = 1000;
    private const int MaximumFileNodes = 2000;
    private const int MaximumFilesPerDirectory = 100;
    private const int MaximumTreeDepth = 16;

    private static readonly TimeSpan CacheRefreshInterval = TimeSpan.FromSeconds(20);

    private readonly string _root = Path.GetFullPath(environment.WebRootPath);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly CancellationTokenSource _stopping = new();
    private Task? _refreshTask;
    private long _version;

    public void Invalidate() => Interlocked.Increment(ref _version);

    public async Task<DirectoryTreeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        if (TryGetSnapshot(Volatile.Read(ref _version), out var cached))
        {
            return cached.Snapshot;
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
                return stale.Snapshot;
            }

            throw;
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
                    logger.LogWarning(
                        exception,
                        "Periodic MediaStorage directory tree refresh failed."
                    );
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal host shutdown.
        }
    }

    private async Task<DirectoryTreeSnapshot> RefreshSnapshotAsync(
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
                    return cached.Snapshot;
                }

                var snapshot = await Task.Run(
                    () => BuildSnapshot(cancellationToken),
                    cancellationToken
                );

                if (version != Volatile.Read(ref _version))
                {
                    forceRefresh = false;
                    continue;
                }

                memoryCache.Set(CacheKey, new CacheSnapshot(version, snapshot));
                return snapshot;
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

    private DirectoryTreeSnapshot BuildSnapshot(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_root) || IsReparsePoint(_root))
        {
            return new DirectoryTreeSnapshot([], []);
        }

        var remainingFiles = MaximumFileNodes;
        var rootFiles = ReadFiles(_root, string.Empty, ref remainingFiles, cancellationToken);
        var directories = ReadDirectoryTree(cancellationToken, ref remainingFiles);
        return new DirectoryTreeSnapshot(directories, rootFiles);
    }

    /// <summary>
    /// Reads the directory hierarchy breadth-first with a budget that only
    /// directory nodes can consume. Files use a separate budget with a
    /// per-directory cap, so a single huge folder cannot hide its siblings.
    /// </summary>
    private IReadOnlyList<DirectoryTreeNode> ReadDirectoryTree(
        CancellationToken cancellationToken,
        ref int remainingFiles
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var remainingDirectories = MaximumDirectoryNodes;
        var queue = new Queue<DirectoryWorkItem>();
        var roots = new List<DirectoryBuilder>();

        foreach (var path in EnumerateDirectories(_root, string.Empty))
        {
            if (remainingDirectories <= 0)
            {
                break;
            }

            remainingDirectories--;
            var name = Path.GetFileName(path);
            var builder = new DirectoryBuilder(name, MediaPath.Combine(string.Empty, name));
            roots.Add(builder);
            queue.Enqueue(new DirectoryWorkItem(path, builder, 0));
        }

        while (queue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = queue.Dequeue();

            item.Builder.Files.AddRange(
                ReadFiles(
                    item.FullPath,
                    item.Builder.RelativePath,
                    ref remainingFiles,
                    cancellationToken
                )
            );

            if (item.Depth >= MaximumTreeDepth)
            {
                continue;
            }

            foreach (var path in EnumerateDirectories(item.FullPath, item.Builder.RelativePath))
            {
                if (remainingDirectories <= 0)
                {
                    return roots.Select(builder => builder.ToNode()).ToArray();
                }

                remainingDirectories--;
                var name = Path.GetFileName(path);
                var child = new DirectoryBuilder(
                    name,
                    MediaPath.Combine(item.Builder.RelativePath, name)
                );
                item.Builder.Children.Add(child);
                queue.Enqueue(new DirectoryWorkItem(path, child, item.Depth + 1));
            }
        }

        return roots.Select(builder => builder.ToNode()).ToArray();
    }

    private string[] EnumerateDirectories(string fullDirectory, string relativeDirectory)
    {
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
            logger.LogDebug(
                exception,
                "Cannot enumerate MediaStorage directory {Directory}.",
                relativeDirectory
            );
            return [];
        }
        catch (IOException exception)
        {
            logger.LogDebug(
                exception,
                "Cannot enumerate MediaStorage directory {Directory}.",
                relativeDirectory
            );
            return [];
        }

        return paths
            .Select(path => new { FullPath = path, Name = Path.GetFileName(path) })
            .Where(directory =>
                MediaPath.IsVisibleSegment(directory.Name)
                && IsWithinRoot(directory.FullPath)
                && !IsReparsePoint(directory.FullPath)
            )
            .OrderBy(directory => directory.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(directory => directory.Name, StringComparer.Ordinal)
            .Select(directory => directory.FullPath)
            .ToArray();
    }

    private IReadOnlyList<FileTreeNode> ReadFiles(
        string fullDirectory,
        string relativeDirectory,
        ref int remainingFiles,
        CancellationToken cancellationToken
    )
    {
        var budget = Math.Min(MaximumFilesPerDirectory, remainingFiles);
        if (budget <= 0)
        {
            return [];
        }

        string[] paths;
        try
        {
            paths = Directory
                .EnumerateFiles(fullDirectory, "*", SearchOption.TopDirectoryOnly)
                .ToArray();
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogDebug(
                exception,
                "Cannot enumerate files in MediaStorage directory {Directory}.",
                relativeDirectory
            );
            return [];
        }
        catch (IOException exception)
        {
            logger.LogDebug(
                exception,
                "Cannot enumerate files in MediaStorage directory {Directory}.",
                relativeDirectory
            );
            return [];
        }

        var files = paths
            .Select(path => new { FullPath = path, Name = Path.GetFileName(path) })
            .Where(file =>
                MediaPath.IsVisibleSegment(file.Name)
                && IsWithinRoot(file.FullPath)
                && !IsReparsePoint(file.FullPath)
            )
            .OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(file => file.Name, StringComparer.Ordinal)
            .ToArray();

        var result = new List<FileTreeNode>(Math.Min(files.Length, budget));
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (result.Count >= budget)
            {
                break;
            }

            result.Add(
                new FileTreeNode(file.Name, MediaPath.Combine(relativeDirectory, file.Name))
            );
        }

        remainingFiles -= result.Count;
        return result;
    }

    private sealed class DirectoryBuilder(string name, string relativePath)
    {
        public string Name { get; } = name;
        public string RelativePath { get; } = relativePath;
        public List<DirectoryBuilder> Children { get; } = [];
        public List<FileTreeNode> Files { get; } = [];

        public DirectoryTreeNode ToNode() =>
            new(
                Name,
                RelativePath,
                Children.Select(child => child.ToNode()).ToArray(),
                Files.ToArray()
            );
    }

    private readonly record struct DirectoryWorkItem(
        string FullPath,
        DirectoryBuilder Builder,
        int Depth
    );

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

    private sealed record CacheSnapshot(long Version, DirectoryTreeSnapshot Snapshot);

    public sealed record DirectoryTreeSnapshot(
        IReadOnlyList<DirectoryTreeNode> Directories,
        IReadOnlyList<FileTreeNode> Files
    );

    public sealed record DirectoryTreeNode(
        string Name,
        string RelativePath,
        IReadOnlyList<DirectoryTreeNode> Children,
        IReadOnlyList<FileTreeNode> Files
    );

    public sealed record FileTreeNode(string Name, string RelativePath);
}
