using System.Security.Cryptography;
using System.Text;
using MARS.MediaStorage.Application.Media;
using MARS.MediaStorage.Application.Storage;
using MARS.MediaStorage.Host.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MARS.MediaStorage.Host.Services;

/// <summary>
/// Следит за webroot, обновляет индекс PostgreSQL и уведомляет открытые клиенты.
/// </summary>
/// <remarks>
/// FileSystemWatcher не является надёжным журналом изменений: события могут
/// теряться, а Linux-контейнеры не всегда получают inotify от Windows bind mount.
/// Поэтому дополнительно сравнивается снимок структуры каталогов раз в минуту.
/// </remarks>
public sealed class MediaStorageFileWatcher(
    IWebHostEnvironment environment,
    IServiceScopeFactory scopeFactory,
    IHubContext<MediaStorageHub> hubContext,
    ILogger<MediaStorageFileWatcher> logger
) : BackgroundService
{
    private static readonly TimeSpan DebounceInterval = TimeSpan.FromMilliseconds(750);
    private static readonly TimeSpan ReconcileInterval = TimeSpan.FromMinutes(1);

    private readonly string _root = Path.GetFullPath(environment.WebRootPath);
    private readonly SemaphoreSlim _refreshSignal = new(0, 1);
    private int _refreshQueued;
    private int _structureRefreshQueued;
    private string? _directorySnapshot;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Directory.Exists(_root) || IsReparsePoint(_root))
        {
            logger.LogError("MediaStorage watcher не запущен: webroot недоступен.");
            return;
        }

        using var watcher = CreateWatcher();
        using var timer = new PeriodicTimer(ReconcileInterval);

        // Первичная сверка подхватывает файлы, появившиеся до запуска watcher.
        _directorySnapshot = CaptureDirectorySnapshot();
        await ReconcileAsync(stoppingToken);

        var signalWait = _refreshSignal.WaitAsync(stoppingToken);
        var timerWait = timer.WaitForNextTickAsync(stoppingToken).AsTask();

        while (!stoppingToken.IsCancellationRequested)
        {
            var completed = await Task.WhenAny(signalWait, timerWait);

            if (completed == signalWait)
            {
                await signalWait;
                Interlocked.Exchange(ref _refreshQueued, 0);
                await WaitForQuietPeriodAsync(stoppingToken);
                signalWait = _refreshSignal.WaitAsync(stoppingToken);
            }
            else
            {
                if (!await timerWait)
                {
                    break;
                }

                timerWait = timer.WaitForNextTickAsync(stoppingToken).AsTask();
            }

            await ReconcileAsync(stoppingToken);
        }
    }

    private FileSystemWatcher? CreateWatcher()
    {
        try
        {
            var watcher = new FileSystemWatcher(_root)
            {
                IncludeSubdirectories = true,
                Filter = "*",
                NotifyFilter =
                    NotifyFilters.FileName
                    | NotifyFilters.DirectoryName
                    | NotifyFilters.LastWrite
                    | NotifyFilters.Size,
                InternalBufferSize = 64 * 1024,
            };

            watcher.Created += OnFileChanged;
            watcher.Changed += OnFileChanged;
            watcher.Deleted += OnFileChanged;
            watcher.Renamed += OnFileRenamed;
            watcher.Error += OnWatcherError;
            watcher.EnableRaisingEvents = true;

            logger.LogInformation("MediaStorage watcher запущен.");

            return watcher;
        }
        catch (Exception ex)
        {
            // Периодическая сверка остаётся резервным механизмом, если платформа
            // или bind mount не позволяют подписаться на файловые события.
            logger.LogWarning(ex, "Не удалось создать FileSystemWatcher для MediaStorage.");
            return null;
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs args)
    {
        if (ShouldObserve(args.FullPath))
        {
            // Сверка каталожного снимка в ReconcileAsync определит, изменилась
            // ли структура. Событие могло относиться только к файлу.
            QueueRefresh();
        }
    }

    private void OnFileRenamed(object sender, RenamedEventArgs args)
    {
        if (ShouldObserve(args.FullPath) || ShouldObserve(args.OldFullPath))
        {
            // Сравнение снимков каталогов отличит rename папки от rename файла.
            QueueRefresh();
        }
    }

    private void OnWatcherError(object sender, ErrorEventArgs args)
    {
        // При переполнении буфера часть уведомлений потеряна, поэтому нужна
        // полная сверка и обновление обоих представлений.
        logger.LogWarning(args.GetException(), "FileSystemWatcher потерял события.");
        QueueRefresh(structureChanged: true);
    }

    private bool ShouldObserve(string fullPath)
    {
        var relativePath = Path.GetRelativePath(_root, fullPath);
        if (
            relativePath == "."
            || Path.IsPathRooted(relativePath)
            || !MediaPath.IsVisibleRelativePath(relativePath)
        )
        {
            return false;
        }

        return true;
    }

    private void QueueRefresh(bool structureChanged = false)
    {
        if (structureChanged)
        {
            Interlocked.Increment(ref _structureRefreshQueued);
        }

        if (Interlocked.Exchange(ref _refreshQueued, 1) != 0)
        {
            return;
        }

        try
        {
            _refreshSignal.Release();
        }
        catch (SemaphoreFullException)
        {
            // Несколько событий могут прийти одновременно; одной сверки достаточно.
        }
        catch (ObjectDisposedException)
        {
            // Событие пришло во время завершения hosted service.
        }
    }

    private async Task WaitForQuietPeriodAsync(CancellationToken cancellationToken)
    {
        // События от одного копирования объединяются в одну индексацию.
        while (true)
        {
            await Task.Delay(DebounceInterval, cancellationToken);

            if (!_refreshSignal.Wait(0))
            {
                return;
            }

            Interlocked.Exchange(ref _refreshQueued, 0);
        }
    }

    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        try
        {
            var nextDirectorySnapshot = CaptureDirectorySnapshot();
            var directoryStructureChanged =
                _directorySnapshot is not null
                && !string.Equals(
                    _directorySnapshot,
                    nextDirectorySnapshot,
                    StringComparison.Ordinal
                );
            var requestedStructureRefresh = Volatile.Read(ref _structureRefreshQueued);

            await using var scope = scopeFactory.CreateAsyncScope();
            var storage = scope.ServiceProvider.GetRequiredService<IMediaStorageService>();
            var result = await storage.IndexAsync(cancellationToken);

            if (result.Added > 0 || result.Updated > 0)
            {
                await hubContext.Clients.All.SendAsync(
                    "StorageChanged",
                    new { result.Added, result.Updated },
                    cancellationToken
                );

                logger.LogInformation(
                    "MediaStorage индекс обновлён: добавлено {Added}, обновлено {Updated}",
                    result.Added,
                    result.Updated
                );
            }

            if (directoryStructureChanged || requestedStructureRefresh > 0)
            {
                await hubContext.Clients.All.SendAsync(
                    "StorageStructureChanged",
                    new { },
                    cancellationToken
                );
            }

            // Не фиксируем снимок до успешной индексации и SignalR-уведомления:
            // при временной ошибке следующая сверка должна повторить событие.
            _directorySnapshot = nextDirectorySnapshot;
            if (requestedStructureRefresh > 0)
            {
                Interlocked.CompareExchange(
                    ref _structureRefreshQueued,
                    0,
                    requestedStructureRefresh
                );
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось пересканировать структуру MediaStorage.");
        }
    }

    private string CaptureDirectorySnapshot()
    {
        var directories = new List<string>();
        var pending = new Stack<string>();
        pending.Push(_root);

        while (pending.TryPop(out var current))
        {
            foreach (
                var child in Directory.EnumerateDirectories(
                    current,
                    "*",
                    SearchOption.TopDirectoryOnly
                )
            )
            {
                var name = Path.GetFileName(child);
                if (!MediaPath.IsVisibleSegment(name) || IsReparsePoint(child))
                {
                    continue;
                }

                var relative = Path.GetRelativePath(_root, child).Replace('\\', '/');
                if (!MediaPath.IsVisibleRelativePath(relative))
                {
                    continue;
                }

                directories.Add(relative);
                pending.Push(child);
            }
        }

        var snapshot = string.Join("\n", directories.OrderBy(path => path, StringComparer.Ordinal));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot)));
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
    }

    public override void Dispose()
    {
        _refreshSignal.Dispose();
        base.Dispose();
    }
}
