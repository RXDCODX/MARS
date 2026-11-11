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
/// Поэтому дополнительно выполняется периодическая полная сверка каталога.
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Directory.Exists(_root))
        {
            logger.LogError("MediaStorage watcher не запущен: webroot не существует: {Root}", _root);
            return;
        }

        using var watcher = CreateWatcher();
        using var timer = new PeriodicTimer(ReconcileInterval);

        // Первичная сверка подхватывает файлы, появившиеся до запуска watcher.
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
            watcher.Renamed += OnFileRenamed;
            watcher.Error += OnWatcherError;
            watcher.EnableRaisingEvents = true;

            logger.LogInformation("MediaStorage watcher наблюдает за {Root}", _root);

            return watcher;
        }
        catch (Exception ex)
        {
            // Периодическая сверка остаётся резервным механизмом, если платформа
            // или bind mount не позволяют подписаться на файловые события.
            logger.LogWarning(ex, "Не удалось создать FileSystemWatcher для {Root}", _root);
            return null;
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs args)
    {
        if (ShouldObserve(args.FullPath))
        {
            QueueRefresh();
        }
    }

    private void OnFileRenamed(object sender, RenamedEventArgs args)
    {
        if (ShouldObserve(args.FullPath) || ShouldObserve(args.OldFullPath))
        {
            QueueRefresh();
        }
    }

    private void OnWatcherError(object sender, ErrorEventArgs args)
    {
        // При переполнении буфера часть уведомлений потеряна, поэтому нужна
        // полная сверка, а не попытка восстановить отдельные события.
        logger.LogWarning(args.GetException(), "FileSystemWatcher потерял события в {Root}", _root);
        QueueRefresh();
    }

    private bool ShouldObserve(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            return false;
        }

        var relativePath = Path.GetRelativePath(_root, fullPath);
        if (
            relativePath == "."
            || relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || Path.IsPathRooted(relativePath)
        )
        {
            return false;
        }

        var segments = relativePath.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries
        );

        // Эти каталоги не входят в индекс MediaStorage. В частности, .git
        // меняется при каждом commit/push, и его события не должны запускать
        // повторную индексацию.
        return !segments.Any(segment =>
            segment.Equals(".git", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("_trash", StringComparison.OrdinalIgnoreCase)
            || segment.StartsWith(".", StringComparison.Ordinal)
        );
    }

    private void QueueRefresh()
    {
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
            await using var scope = scopeFactory.CreateAsyncScope();
            var storage = scope.ServiceProvider.GetRequiredService<IMediaStorageService>();
            var result = await storage.IndexAsync(cancellationToken);

            if (result.Added == 0 && result.Updated == 0)
            {
                return;
            }

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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось пересканировать каталог MediaStorage {Root}", _root);
        }
    }

    public override void Dispose()
    {
        _refreshSignal.Dispose();
        base.Dispose();
    }
}
