using MARS.MediaStorage.DataBaseContext;
using MARS.MediaStorage.Entities;
using MARS.MediaStorage.Services.Git;
using MARS.MediaStorage.Services.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MARS.Shared.Models.Media;

namespace MARS.MediaStorage.Services.Storage;

/// <summary>
/// Итог операции над набором файлов.
/// </summary>
/// <param name="Requested">Сколько записей передали.</param>
/// <param name="Succeeded">Сколько обработано без ошибок.</param>
/// <param name="Failed">Сколько пропущено с ошибкой.</param>
/// <param name="Errors">Человекочитаемые причины пропуска.</param>
public readonly record struct BulkOperationResult(
    int Requested,
    int Succeeded,
    int Failed,
    IReadOnlyList<string> Errors
);

public readonly record struct IndexResult(int Added, int Updated);

public interface IMediaStorageService
{
    Task<IndexResult> IndexAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaStorageEntry>> ListAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default
    );

    Task<BulkOperationResult> SoftDeleteAsync(
        IReadOnlyCollection<Guid> ids,
        bool dryRun = false,
        CancellationToken cancellationToken = default
    );

    Task<int> RestoreAsync(
        IReadOnlyCollection<Guid> ids,
        bool dryRun = false,
        CancellationToken cancellationToken = default
    );

    Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Приём файлов в хранилище.
    /// </summary>
    Task<BulkOperationResult> UploadAsync(
        IReadOnlyCollection<MediaUploadFile> files,
        string targetDirectory,
        CancellationToken cancellationToken = default
    );

    Task<BulkOperationResult> MoveAsync(
        IReadOnlyCollection<Guid> ids,
        string targetDirectory,
        bool dryRun = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Фиксирует факт выдачи файла. Ошибка фиксации не должна ломать отдачу:
    /// дата — вспомогательные данные, а не условие успешного ответа.
    /// </summary>
    Task MarkDownloadedAsync(
        string relativePath,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// Управление файловым хранилищем wwwroot: индексация, мягкое удаление,
/// восстановление и массовые перемещения.
/// </summary>
/// <remarks>
/// Аудит Stage 3: раньше хранилище было просто папкой — удаление было
/// необратимым, а даты загрузки и выгрузки не фиксировались. Операции идут
/// через одну точку, которая всегда синхронизирует диск, БД и git, чтобы
/// состояния не расходились.
/// </remarks>
public sealed class MediaStorageService(
    IDbContextFactory<MediaStorageDbContext> factory,
    string webRootPath,
    IMediaGitService gitService,
    TimeProvider timeProvider,
    TimeSpan trashRetention,
    long maxUploadBytes,
    ILogger<MediaStorageService> logger
) : IMediaStorageService
{
    private string FullPath(string relativePath) =>
        Path.GetFullPath(
            Path.Combine(
                webRootPath,
                MediaPath.Normalize(relativePath).Replace('/', Path.DirectorySeparatorChar)
            )
        );

    public async Task<IndexResult> IndexAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(webRootPath))
        {
            return new IndexResult(0, 0);
        }

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var known = await db.MediaEntries.ToDictionaryAsync(e => e.Path, cancellationToken);

        var added = 0;
        var updated = 0;

        foreach (var fullPath in EnumerateIndexableFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relative = Path.GetRelativePath(webRootPath, fullPath).Replace('\\', '/');
            var file = new FileInfo(fullPath);

            if (known.TryGetValue(relative, out var entry))
            {
                // Размер пересчитываем всегда: файл мог измениться извне.
                if (entry.SizeBytes != file.Length)
                {
                    entry.SizeBytes = file.Length;
                    updated++;
                }

                continue;
            }

            db.MediaEntries.Add(
                new MediaStorageEntry
                {
                    Path = relative,
                    FileName = file.Name,
                    Extension = file.Extension,
                    MediaType = ResolveMediaType(file.Extension),
                    SizeBytes = file.Length,
                    // Для уже существующих файлов датой загрузки считаем время
                    // изменения файла: иначе дата была бы «сейчас» при первом
                    // сканировании и ничего не значила.
                    UploadedAt = new DateTimeOffset(
                        file.LastWriteTimeUtc,
                        TimeSpan.Zero
                    ),
                }
            );

            known[relative] = entry!;
            added++;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new IndexResult(added, updated);
    }

    public async Task<IReadOnlyList<MediaStorageEntry>> ListAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var query = db.MediaEntries.AsNoTracking();

        if (!includeDeleted)
        {
            query = query.Where(e => e.DeletedAt == null);
        }

        return await query.OrderBy(e => e.Path).ToListAsync(cancellationToken);
    }

    public async Task<BulkOperationResult> SoftDeleteAsync(
        IReadOnlyCollection<Guid> ids,
        bool dryRun = false,
        CancellationToken cancellationToken = default
    )
    {
        if (ids.Count == 0)
        {
            return new BulkOperationResult(0, 0, 0, []);
        }

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var entries = await db.MediaEntries
            .Where(e => ids.Contains(e.Id) && e.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();
        var errors = new List<string>();
        var succeeded = 0;

        foreach (var entry in entries)
        {
            if (TrashPathBuilder.IsUnderTrash(entry.Path))
            {
                continue;
            }

            var source = FullPath(entry.Path);

            if (!File.Exists(source))
            {
                errors.Add($"{entry.Path}: файл отсутствует на диске");
                continue;
            }

            var trashPath = TrashPathBuilder.BuildTrashPath(entry.Path, now);
            var target = FullPath(trashPath);

            if (dryRun)
            {
                succeeded++;
                continue;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Move(source, target);

                entry.OriginalPath = entry.Path;
                entry.Path = trashPath;
                entry.DeletedAt = now;
                succeeded++;
            }
            catch (IOException ex)
            {
                errors.Add($"{entry.Path}: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                errors.Add($"{entry.Path}: {ex.Message}");
            }
        }

        var missing = ids.Count - entries.Count;
        if (missing > 0)
        {
            errors.Add($"не найдено или уже удалено записей: {missing}");
        }

        if (!dryRun && succeeded > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            await CommitAsync($"Удалено в корзину: {succeeded}", cancellationToken);
        }

        return new BulkOperationResult(ids.Count, succeeded, missing, errors);
    }

    public async Task<int> RestoreAsync(
        IReadOnlyCollection<Guid> ids,
        bool dryRun = false,
        CancellationToken cancellationToken = default
    )
    {
        if (ids.Count == 0)
        {
            return 0;
        }

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var entries = await db.MediaEntries
            .Where(e => ids.Contains(e.Id) && e.DeletedAt != null)
            .ToListAsync(cancellationToken);

        var restored = 0;

        foreach (var entry in entries)
        {
            var original = entry.OriginalPath;

            if (string.IsNullOrWhiteSpace(original) || !MediaPath.IsSafeRelative(original))
            {
                continue;
            }

            var source = FullPath(entry.Path);
            var target = FullPath(original);

            // Восстановление не перезаписывает: занятый путь означает, что
            // на месте удалённого файла уже лежит другой.
            if (File.Exists(target))
            {
                continue;
            }

            if (dryRun)
            {
                restored++;
                continue;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Move(source, target);

                entry.Path = original;
                entry.OriginalPath = null;
                entry.DeletedAt = null;
                restored++;
            }
            catch (IOException)
            {
                // Файл занят или недоступен — оставляем в корзине.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        if (!dryRun && restored > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            await CommitAsync($"Восстановлено из корзины: {restored}", cancellationToken);
        }

        return restored;
    }

    public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();

        var expired = await db.MediaEntries
            .Where(e => e.DeletedAt != null)
            .ToListAsync(cancellationToken);

        var purged = 0;

        foreach (var entry in expired)
        {
            if (!entry.IsPurgeable(now, trashRetention))
            {
                continue;
            }

            var file = FullPath(entry.Path);

            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }

                db.MediaEntries.Remove(entry);
                purged++;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        if (purged > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            await CommitAsync($"Безвозвратно удалено из корзины: {purged}", cancellationToken);
        }

        return purged;
    }

    public async Task<BulkOperationResult> UploadAsync(
        IReadOnlyCollection<MediaUploadFile> files,
        string targetDirectory,
        CancellationToken cancellationToken = default
    )
    {
        var errors = new List<string>();

        if (files.Count == 0)
        {
            return new BulkOperationResult(0, 0, 0, errors);
        }

        // Каталог нормализуем до проверки: «/Alerts/videos» — обычный ввод
        // пользователя, а не абсолютный путь. IsSafeRelative отверг бы его
        // вместе с ведущим слешем. «..» при нормализации сохраняется и
        // отсекается уже самой проверкой.
        var normalizedDirectory = MediaPath.Normalize(targetDirectory);

        if (
            string.IsNullOrWhiteSpace(normalizedDirectory)
            || !MediaPath.IsSafeRelative(normalizedDirectory)
        )
        {
            errors.Add($"недопустимый каталог: '{targetDirectory}'");
            return new BulkOperationResult(files.Count, 0, files.Count, errors);
        }

        if (TrashPathBuilder.IsUnderTrash(normalizedDirectory))
        {
            errors.Add("загрузка напрямую в корзину запрещена");
            return new BulkOperationResult(files.Count, 0, files.Count, errors);
        }

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var uploaded = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Имя приходит от клиента и целиком недоверенно. Path.GetFileName
            // молча срезал бы «../» и файл загрузился бы под другим именем —
            // пользователь получил бы не то, что отправил. Поэтому путь в
            // имени отвергаем явно, а не чиним молча.
            var rawName = file.FileName ?? string.Empty;

            if (
                string.IsNullOrWhiteSpace(rawName)
                || rawName.Contains('/')
                || rawName.Contains('\\')
                || MediaPath.Normalize(rawName).Contains("..")
            )
            {
                errors.Add($"недопустимое имя файла: '{rawName}'");
                continue;
            }

            var fileName = rawName;
            var relativePath = MediaPath.Combine(normalizedDirectory, fileName);
            var full = FullPath(relativePath);

            if (File.Exists(full))
            {
                errors.Add($"{relativePath}: файл уже существует");
                continue;
            }

            long written;
            string hash;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                (written, hash) = await WriteFileAsync(full, file.Content, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add($"{relativePath}: {ex.Message}");
                continue;
            }

            if (written == 0)
            {
                errors.Add($"{relativePath}: файл пустой");
                TryDelete(full);
                continue;
            }

            if (written > maxUploadBytes)
            {
                // Файл уже на диске, но в репозиторий его положить нельзя:
                // GitHub отклоняет объекты крупнее 100 МБ, и push сломался бы.
                errors.Add(
                    $"{relativePath}: {written / (1024 * 1024)} МБ превышает лимит "
                    + $"{maxUploadBytes / (1024 * 1024)} МБ, загрузка отклонена"
                );
                TryDelete(full);
                continue;
            }

            var existing = await db.MediaEntries
                .FirstOrDefaultAsync(e => e.Path == relativePath, cancellationToken);

            if (existing is not null)
            {
                existing.SizeBytes = written;
                existing.ContentHash = hash;
                existing.UploadedAt = now;
            }
            else
            {
                db.MediaEntries.Add(
                    new MediaStorageEntry
                    {
                        Path = relativePath,
                        FileName = fileName,
                        Extension = Path.GetExtension(fileName),
                        MediaType = ResolveMediaType(Path.GetExtension(fileName)),
                        SizeBytes = written,
                        UploadedAt = now,
                        ContentHash = hash,
                    }
                );
            }

            uploaded++;
        }

        if (uploaded > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            await CommitAsync($"Загружено в {targetDirectory}: {uploaded}", cancellationToken);
        }

        return new BulkOperationResult(files.Count, uploaded, files.Count - uploaded, errors);
    }

    /// <summary>
    /// Пишет файл на диск, попутно считая размер и SHA-256 одним проходом.
    /// </summary>
    private static async Task<(long Size, string Hash)> WriteFileAsync(
        string path,
        Stream source,
        CancellationToken cancellationToken
    )
    {
        using var hash = System.Security.Cryptography.SHA256.Create();
        using var target = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true
        );

        var buffer = new byte[81920];
        long total = 0;

        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            hash.TransformBlock(buffer, 0, read, null, 0);
            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            total += read;
        }

        hash.TransformFinalBlock([], 0, 0);

        return (total, Convert.ToHexString(hash.Hash!).ToLowerInvariant());
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }


    public async Task<BulkOperationResult> MoveAsync(
        IReadOnlyCollection<Guid> ids,
        string targetDirectory,
        bool dryRun = false,
        CancellationToken cancellationToken = default
    )
    {
        var errors = new List<string>();

        if (ids.Count == 0)
        {
            return new BulkOperationResult(0, 0, 0, errors);
        }

        // Целевой каталог проверяем до похода в БД: запись вида «../../outside»
        // не должна ни переместить файл, ни оставить запись в БД изменённой.
        if (!MediaPath.IsSafeRelative(targetDirectory) || string.IsNullOrWhiteSpace(targetDirectory))
        {
            errors.Add($"недопустимый каталог назначения: '{targetDirectory}'");
            return new BulkOperationResult(ids.Count, 0, ids.Count, errors);
        }

        if (TrashPathBuilder.IsUnderTrash(targetDirectory))
        {
            errors.Add("перенос в корзину выполняйте через удаление, а не через move");
            return new BulkOperationResult(ids.Count, 0, ids.Count, errors);
        }

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var entries = await db.MediaEntries
            .Where(e => ids.Contains(e.Id) && e.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var moved = 0;

        foreach (var entry in entries)
        {
            var source = FullPath(entry.Path);
            var target = MediaPath.Combine(targetDirectory, entry.FileName);
            var targetFull = FullPath(target);

            if (!File.Exists(source))
            {
                errors.Add($"{entry.Path}: файл отсутствует на диске");
                continue;
            }

            if (File.Exists(targetFull))
            {
                errors.Add($"{target}: файл уже существует");
                continue;
            }

            if (string.Equals(entry.Path, target, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (dryRun)
            {
                moved++;
                continue;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetFull)!);
                File.Move(source, targetFull);

                entry.Path = target;
                moved++;
            }
            catch (IOException ex)
            {
                errors.Add($"{entry.Path}: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                errors.Add($"{entry.Path}: {ex.Message}");
            }
        }

        if (!dryRun && moved > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            await CommitAsync(
                $"Перемещено в {targetDirectory}: {moved}",
                cancellationToken
            );
        }

        var failed = ids.Count - moved;

        return new BulkOperationResult(ids.Count, moved, failed, errors);
    }

    private async Task CommitAsync(string message, CancellationToken cancellationToken)
    {
        var result = await gitService.SyncAsync(message, cancellationToken: cancellationToken);

        if (!result.Success)
        {
            // Операция с диском и БД уже применена. Падать из-за git нельзя —
            // рассинхронизация версий не должна ронять отдачу медиа, но молчать
            // тоже нельзя: без записи операция потеряется при следующем пуше.
            logger.LogError(
                "Не удалось зафиксировать изменение хранилища в git: {Message}. Ошибка: {Error}",
                message,
                result.Error
            );
        }
    }

    public async Task MarkDownloadedAsync(
        string relativePath,
        CancellationToken cancellationToken = default
    )
    {
        var path = MediaPath.Normalize(relativePath);

        if (path.Length == 0)
        {
            return;
        }

        try
        {
            await using var db = await factory.CreateDbContextAsync(cancellationToken);
            var entry = await db.MediaEntries.FirstOrDefaultAsync(e => e.Path == path, cancellationToken);

            if (entry is null)
            {
                return;
            }

            entry.LastDownloadedAt = timeProvider.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Не удалось зафиксировать дату выгрузки {Path}", path);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось зафиксировать дату выгрузки {Path}", path);
        }
    }

    private IEnumerable<string> EnumerateIndexableFiles()
    {
        foreach (
            var file in Directory.EnumerateFiles(
                webRootPath,
                "*",
                SearchOption.AllDirectories
            )
        )
        {
            var relative = Path.GetRelativePath(webRootPath, file).Replace('\\', '/');

            if (relative.StartsWith(TrashPathBuilder.TrashFolder + "/", StringComparison.Ordinal)
                || relative.Equals(TrashPathBuilder.TrashFolder, StringComparison.Ordinal))
            {
                continue;
            }

            if (relative.StartsWith(".", StringComparison.Ordinal))
            {
                continue;
            }

            if (relative.Contains('/' + TrashPathBuilder.TrashFolder + '/', StringComparison.Ordinal))
            {
                continue;
            }

            yield return file;
        }
    }

    private static MediaType ResolveMediaType(string extension) =>
        extension.ToLowerInvariant() switch
        {
            ".mp4" or ".webm" or ".avi" or ".mov" or ".wmv" or ".mkv" => MediaType.Video,
            ".mp3" or ".wav" or ".ogg" or ".opus" or ".flac" => MediaType.Audio,
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".svg" or ".bmp" => MediaType.Image,
            _ => MediaType.None,
        };
}
