using MARS.MediaStorage.Application.Alerts;
using MARS.MediaStorage.Application.Extensions;
using MARS.MediaStorage.Application.Media;
using MARS.MediaStorage.Application.Storage;
using MARS.MediaStorage.Infrastructure.Postgres.Data;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MARS.MediaStorage.Infrastructure.Postgres.Alerts;

/// <summary>
/// Сценарии работы с алертами: чтение, создание, обновление, удаление и
/// отдача прикреплённого файла. Контроллер оставляет за собой только HTTP и
/// маппинг <see cref="ApiMediaInfo"/> ↔ <see cref="MediaInfo"/>.
/// </summary>
public sealed class AlertService(
    IDbContextFactory<MediaStorageDbContext> factory,
    ILogger<AlertService> logger,
    IMediaFileStorageService storage,
    IMediaStorageService mediaStorage,
    IMediaInspector inspector,
    IMediaTranscoder transcoder,
    IWebHostEnvironment webHostEnvironment
) : IAlertService
{
    public async Task<IReadOnlyList<MediaInfo>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Alerts.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<MediaInfo?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Alerts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<AlertFileLookup> GetFileAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

        var alert = await dbContext.Alerts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (alert == null)
        {
            return NotFound($"Алерт с ID '{id}' не найден");
        }

        var filePath = alert.FileInfo.FilePath;

        if (string.IsNullOrEmpty(filePath))
        {
            return NotFound("Путь к файлу не найден");
        }

        if (filePath.StartsWith("memory/"))
        {
            return NotFound("Файлы в памяти пока не поддерживаются");
        }

        var fullPath = ResolveMediaPath(filePath);

        if (!File.Exists(fullPath))
        {
            return NotFound($"Файл не найден по пути: {fullPath}");
        }

        // Аудит Stage 3: дата выгрузки не фиксировалась, в хранилище не было
        // видно, что вообще читают.
        await mediaStorage.MarkDownloadedAsync(filePath, cancellationToken);

        return new AlertFileLookup(
            AlertFileLookupStatus.Found,
            fullPath,
            MediaPath.GetContentType(alert.FileInfo.Extension),
            alert.FileInfo.FileName
        );
    }

    public async Task<AlertSaveResult> CreateAsync(
        MediaInfo alert,
        IFormFile? file,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsFreezeRuleValid(alert.MetaInfo))
        {
            return AlertSaveResult.Fail(
                "IsFreezeRequired может быть true только когда Priority = High"
            );
        }

        if (file is null)
        {
            return AlertSaveResult.Fail("Файл не передан");
        }

        if (
            !TryResolveUploadedMemsFilePath(
                alert.FileInfo.FilePath,
                out var targetFilePath,
                out var pathError
            )
        )
        {
            return AlertSaveResult.Fail(pathError);
        }

        var fileInfo = await storage.SaveFileAsync(file, targetFilePath);

        await PrepareFileAsync(fileInfo);

        var createdAlert = CreateStoredAlert(alert, fileInfo);

        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

        dbContext.Alerts.Add(createdAlert);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AlertSaveResult.Ok(createdAlert);
    }

    public async Task<AlertSaveResult> UpdateAsync(
        Guid id,
        MediaInfo alert,
        IFormFile? file,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsFreezeRuleValid(alert.MetaInfo))
        {
            return AlertSaveResult.Fail(
                "IsFreezeRequired может быть true только когда Priority = High"
            );
        }

        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

        var existingAlert = await dbContext.Alerts.FirstOrDefaultAsync(
            a => a.Id == id,
            cancellationToken
        );

        if (existingAlert == null)
        {
            return AlertSaveResult.Fail($"Алерт с ID '{id}' не найден");
        }

        var resolvedFileInfo = alert.FileInfo;

        if (file is not null)
        {
            if (
                !TryResolveUploadedMemsFilePath(
                    alert.FileInfo.FilePath,
                    out var targetFilePath,
                    out var pathError
                )
            )
            {
                return AlertSaveResult.Fail(pathError);
            }

            resolvedFileInfo = await storage.SaveFileAsync(file, targetFilePath);

            await PrepareFileAsync(resolvedFileInfo);

            var oldPath = existingAlert.FileInfo.FilePath ?? string.Empty;
            if (
                !string.IsNullOrWhiteSpace(oldPath)
                && !oldPath.StartsWith("memory/", StringComparison.OrdinalIgnoreCase)
                && oldPath.StartsWith("/", StringComparison.Ordinal)
            )
            {
                var oldFullPath = ResolveMediaPath(oldPath);

                if (File.Exists(oldFullPath))
                {
                    File.Delete(oldFullPath);
                }
            }
        }
        else
        {
            var oldPath = existingAlert.FileInfo.FilePath ?? string.Empty;
            var newPath = resolvedFileInfo.FilePath ?? string.Empty;

            if (
                !string.IsNullOrWhiteSpace(oldPath)
                && !string.IsNullOrWhiteSpace(newPath)
                && !string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase)
                && !oldPath.StartsWith("memory/", StringComparison.OrdinalIgnoreCase)
                && !newPath.StartsWith("memory/", StringComparison.OrdinalIgnoreCase)
            )
            {
                // Аудит: источник искался перебором CWD/AppContext.BaseDirectory
                // с приклейкой "wwwroot". Это давало разные ответы в
                // зависимости от того, откуда запущен процесс, и не находило
                // файл, если WebRootPath отличался.
                var oldFullPath = ResolveMediaPath(oldPath);

                if (!File.Exists(oldFullPath))
                {
                    return AlertSaveResult.Fail("Файл для перемещения не найден");
                }

                var newFullPath = ResolveMediaPath(newPath);

                var newDirectory = Path.GetDirectoryName(newFullPath);
                if (!string.IsNullOrWhiteSpace(newDirectory))
                {
                    Directory.CreateDirectory(newDirectory);
                }

                File.Move(oldFullPath, newFullPath, true);
            }
        }

        var updatedAlert = CreateStoredAlert(alert, resolvedFileInfo);

        dbContext.Entry(existingAlert).State = EntityState.Detached;
        dbContext.Alerts.Update(updatedAlert);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AlertSaveResult.Ok(updatedAlert);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

        var alert = await dbContext.Alerts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (alert == null)
        {
            return false;
        }

        dbContext.Alerts.Remove(alert);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Транскодирует загруженный файл в играбельный формат и при необходимости
    /// синхронизирует dev-копию. Ошибки подготовки не должны ронять создание
    /// алерта: файл уже сохранён, а инспекция — вспомогательный шаг.
    /// </summary>
    private async Task PrepareFileAsync(MediaFileInfo fileInfo)
    {
        try
        {
            var fullPath = ResolveMediaPath(fileInfo.FilePath);

            var playablePath = await transcoder.EnsurePlayableAsync(fullPath);

            if (!string.Equals(playablePath, fullPath, StringComparison.OrdinalIgnoreCase))
            {
                var rel = ToStorageUrl(playablePath);
                fileInfo.FilePath = rel;
                fileInfo.Extension = Path.GetExtension(playablePath);
                fileInfo.FileName = MediaPath.GetFileName(playablePath);
                fileInfo.Type = await fileInfo.Extension.GetFileMediaTypeAsync();

                try
                {
                    await storage.CopyToDevCopiesAsync(fileInfo.FilePath);
                }
                catch { }
            }
            else
            {
                var probe = await inspector.ProbeAsync(fullPath);
                if (
                    (
                        fileInfo.Type == MediaType.Audio
                        && (probe.BitrateKbps is null || probe.BitrateKbps < 128)
                    )
                    || (
                        fileInfo.Type == MediaType.Video
                        && (probe.BitrateKbps is null || probe.BitrateKbps < 128)
                    )
                )
                {
                    logger.LogInformation(
                        "Загружен файл с низким битрейтом: {File} ({Bitrate} kbps)",
                        fullPath,
                        probe.BitrateKbps
                    );
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(
                ex,
                "Не удалось выполнить пробинг/транскодирование файла {File}",
                fileInfo.FilePath
            );
        }

        if (fileInfo.IsLocalFile)
        {
            try
            {
                await storage.CopyToDevCopiesAsync(fileInfo.FilePath);
            }
            catch (Exception copyEx)
            {
                logger.LogDebug(
                    copyEx,
                    "Не удалось синхронизировать dev-копию файла {File}",
                    fileInfo.FilePath
                );
            }
        }
    }

    /// <summary>
    /// Аудит: пути собирались через <c>Directory.GetCurrentDirectory() + "wwwroot"</c>.
    /// Это работало только если текущий каталог совпадал с корнем приложения.
    /// Единый резолв через <see cref="IWebHostEnvironment.WebRootPath"/> убирает
    /// зависимость от CWD и разнобой разделителей.
    /// </summary>
    private string ResolveMediaPath(string relativePath)
    {
        return Path.GetFullPath(
            Path.Combine(webHostEnvironment.WebRootPath, MediaPath.Normalize(relativePath))
        );
    }

    /// <summary>
    /// Обратная операция: абсолютный путь → путь хранилища с ведущим «/».
    /// </summary>
    private string ToStorageUrl(string fullPath)
    {
        var relative = Path.GetRelativePath(
            webHostEnvironment.WebRootPath,
            Path.GetFullPath(fullPath)
        );

        return "/" + MediaPath.Normalize(relative);
    }

    private static bool TryResolveUploadedMemsFilePath(
        string? filePath,
        out string resolvedPath,
        out string errorMessage
    )
    {
        resolvedPath = string.Empty;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            errorMessage = "Укажи путь к файлу внутри Alerts/uploaded_mems";
            return false;
        }

        var normalized = NormalizeRelativePath(filePath);
        var relative = normalized.TrimStart('/');

        if (Path.IsPathRooted(relative) || relative.Contains("..", StringComparison.Ordinal))
        {
            errorMessage =
                "Путь к файлу должен быть относительным и находиться внутри Alerts/uploaded_mems";
            return false;
        }

        if (!relative.StartsWith("Alerts/uploaded_mems/", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "Для загружаемых файлов используй путь внутри Alerts/uploaded_mems";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Path.GetFileName(relative)))
        {
            errorMessage = "Укажи имя файла в пути Alerts/uploaded_mems";
            return false;
        }

        resolvedPath = normalized;
        return true;
    }

    private static string NormalizeRelativePath(string path)
    {
        var normalized = path.Replace('\\', '/');

        while (normalized.Contains("//", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("//", "/", StringComparison.Ordinal);
        }

        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        return normalized;
    }

    private static MediaInfo CreateStoredAlert(MediaInfo source, MediaFileInfo fileInfo)
    {
        return new MediaInfo
        {
            Id = source.Id,
            TextInfo = source.TextInfo,
            FileInfo = fileInfo,
            PositionInfo = source.PositionInfo,
            MetaInfo = source.MetaInfo,
            StylesInfo = source.StylesInfo,
        };
    }

    private static bool IsFreezeRuleValid(MediaMetaInfo metaInfo)
    {
        return !metaInfo.IsFreezeRequired || metaInfo.Priority == MediaAlertPriority.High;
    }

    private static AlertFileLookup NotFound(string message) =>
        new(AlertFileLookupStatus.NotFound, Message: message);
}
