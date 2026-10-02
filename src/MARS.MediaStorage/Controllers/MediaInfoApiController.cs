using System.Text.Json;
using MARS.MediaStorage.DataBaseContext;
using MARS.MediaStorage.Entities;
using MARS.MediaStorage.Extensions;
using MARS.MediaStorage.Services;
using MARS.MediaStorage.Services.Media;
using MARS.MediaStorage.Services.Storage;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MARS.MediaStorage.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaInfoApiController(
    IDbContextFactory<MediaStorageDbContext> factory,
    ILogger<MediaInfoApiController> logger,
    IMediaFileStorageService storage,
    IMediaStorageService mediaStorage,
    IMediaInspector inspector,
    IMediaTranscoder transcoder,
    IWebHostEnvironment webHostEnvironment
) : ControllerBase
{
    private static readonly JsonSerializerOptions FormJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

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

    [HttpGet]
    public async Task<ActionResult<OperationResult<List<ApiMediaInfo>>>> GetAllAlerts()
    {
        ActionResult<OperationResult<List<ApiMediaInfo>>> result = null!;

        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();

            var alerts = await dbContext.Alerts.ToListAsync();
            var apiAlerts = alerts.Select(a => new ApiMediaInfo(a)).ToList();
            result = Ok(OperationResult<List<ApiMediaInfo>>.Ok(apiAlerts));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении алертов");
            result = Ok(OperationResult<List<ApiMediaInfo>>.Fail("Ошибка при получении алертов"));
        }

        return result;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OperationResult<ApiMediaInfo?>>> GetAlert(Guid id)
    {
        ActionResult<OperationResult<ApiMediaInfo?>> result = null!;

        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();

            var alert = await dbContext.Alerts.FirstOrDefaultAsync(a => a.Id == id);

            if (alert != null)
            {
                result = Ok(OperationResult<ApiMediaInfo?>.Ok(new ApiMediaInfo(alert)));
            }
            else
            {
                result = Ok(OperationResult<ApiMediaInfo?>.Fail($"Алерт с ID '{id}' не найден"));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении алерта {Id}", id);
            result = Ok(OperationResult<ApiMediaInfo?>.Fail("Ошибка при получении алерта"));
        }

        return result;
    }

    [HttpGet("{id:guid}/file")]
    public async Task<ActionResult> GetAlertFile(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult result = null!;

        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();

            var alert = await dbContext.Alerts.FirstOrDefaultAsync(a => a.Id == id);
            if (alert == null)
            {
                result = NotFound($"Алерт с ID '{id}' не найден");
            }
            else
            {
                var filePath = alert.FileInfo.FilePath;
                if (string.IsNullOrEmpty(filePath))
                {
                    result = NotFound("Путь к файлу не найден");
                }
                else if (filePath.StartsWith("memory/"))
                {
                    result = NotFound("Файлы в памяти пока не поддерживаются");
                }
                else
                {
                    var fullPath = ResolveMediaPath(filePath);

                    if (!System.IO.File.Exists(fullPath))
                    {
                        result = NotFound($"Файл не найден по пути: {fullPath}");
                    }
                    else
                    {
                        // Аудит Stage 3: дата выгрузки не фиксировалась, в
                        // хранилище не было видно, что вообще читают.
                        await mediaStorage.MarkDownloadedAsync(filePath, cancellationToken);

                        // Аудит: файл целиком читался в память через ReadAllBytesAsync.
                        // Для крупных видео это исчерпывало память процесса.
                        result = PhysicalFile(
                            fullPath,
                            MediaPath.GetContentType(alert.FileInfo.Extension),
                            alert.FileInfo.FileName,
                            enableRangeProcessing: true
                        );
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении файла алерта {Id}", id);
            result = StatusCode(500, "Внутренняя ошибка сервера");
        }

        return result;
    }

    [HttpPost]
    public async Task<ActionResult<OperationResult<ApiMediaInfo?>>> CreateAlert(
        [FromForm] MediaInfoUpsertRequest request
    )
    {
        ActionResult<OperationResult<ApiMediaInfo?>> result = null!;

        try
        {
            if (string.IsNullOrWhiteSpace(request.AlertJson))
            {
                result = Ok(OperationResult<ApiMediaInfo?>.Fail("Данные алерта не переданы"));
            }
            else
            {
                var alert = JsonSerializer.Deserialize<ApiMediaInfo>(
                    request.AlertJson,
                    FormJsonOptions
                );
                if (alert is null)
                {
                    result = Ok(OperationResult<ApiMediaInfo?>.Fail("Не удалось разобрать алерт"));
                }
                else if (!IsFreezeRuleValid(alert.MetaInfo))
                {
                    result = Ok(
                        OperationResult<ApiMediaInfo?>.Fail(
                            "IsFreezeRequired может быть true только когда Priority = High"
                        )
                    );
                }
                else if (request.File is null)
                {
                    result = Ok(OperationResult<ApiMediaInfo?>.Fail("Файл не передан"));
                }
                else if (
                    !TryResolveUploadedMemsFilePath(
                        alert.FileInfo.FilePath,
                        out var targetFilePath,
                        out var pathError
                    )
                )
                {
                    result = Ok(OperationResult<ApiMediaInfo?>.Fail(pathError));
                }
                else
                {
                    var fileInfo = await storage.SaveFileAsync(request.File, targetFilePath);

                    try
                    {
                        var fullPath = ResolveMediaPath(fileInfo.FilePath);

                        var playablePath = await transcoder.EnsurePlayableAsync(fullPath);

                        if (
                            !string.Equals(
                                playablePath,
                                fullPath,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
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
                    var createdAlert = CreateStoredAlert(alert, fileInfo);

                    await using var dbContext = await factory.CreateDbContextAsync();

                    dbContext.Alerts.Add(createdAlert);
                    await dbContext.SaveChangesAsync();

                    result = Ok(OperationResult<ApiMediaInfo?>.Ok(new ApiMediaInfo(createdAlert)));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании алерта");
            result = Ok(OperationResult<ApiMediaInfo?>.Fail("Ошибка при создании алерта"));
        }

        return result;
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OperationResult<ApiMediaInfo?>>> UpdateAlert(
        Guid id,
        [FromForm] MediaInfoUpsertRequest request
    )
    {
        ActionResult<OperationResult<ApiMediaInfo?>> result = null!;

        try
        {
            if (string.IsNullOrWhiteSpace(request.AlertJson))
            {
                result = Ok(OperationResult<ApiMediaInfo?>.Fail("Данные алерта не переданы"));
            }
            else
            {
                var alert = JsonSerializer.Deserialize<ApiMediaInfo>(
                    request.AlertJson,
                    FormJsonOptions
                );
                if (alert is null)
                {
                    result = Ok(OperationResult<ApiMediaInfo?>.Fail("Не удалось разобрать алерт"));
                }
                else if (!IsFreezeRuleValid(alert.MetaInfo))
                {
                    result = Ok(
                        OperationResult<ApiMediaInfo?>.Fail(
                            "IsFreezeRequired может быть true только когда Priority = High"
                        )
                    );
                }
                else if (id != alert.Id)
                {
                    result = Ok(
                        OperationResult<ApiMediaInfo?>.Fail(
                            "ID в URL не совпадает с ID в теле запроса"
                        )
                    );
                }
                else
                {
                    await using var dbContext = await factory.CreateDbContextAsync();

                    var existingAlert = await dbContext.Alerts.FirstOrDefaultAsync(a => a.Id == id);
                    if (existingAlert == null)
                    {
                        result = Ok(
                            OperationResult<ApiMediaInfo?>.Fail($"Алерт с ID '{id}' не найден")
                        );
                    }
                    else
                    {
                        var resolvedFileInfo = alert.FileInfo;

                        if (request.File is not null)
                        {
                            if (
                                !TryResolveUploadedMemsFilePath(
                                    alert.FileInfo.FilePath,
                                    out var targetFilePath,
                                    out var pathError
                                )
                            )
                            {
                                result = Ok(OperationResult<ApiMediaInfo?>.Fail(pathError));
                                return result;
                            }

                            resolvedFileInfo = await storage.SaveFileAsync(
                                request.File,
                                targetFilePath
                            );

                            try
                            {
                                var fullPath = ResolveMediaPath(resolvedFileInfo.FilePath);

                                var playable = await transcoder.EnsurePlayableAsync(fullPath);
                                if (
                                    !string.Equals(
                                        playable,
                                        fullPath,
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                )
                                {
                                    var rel = ToStorageUrl(playable);
                                    resolvedFileInfo.FilePath = rel;
                                    resolvedFileInfo.Extension = Path.GetExtension(playable);
                                    resolvedFileInfo.FileName = MediaPath.GetFileName(playable);
                                    resolvedFileInfo.Type =
                                        await resolvedFileInfo.Extension.GetFileMediaTypeAsync();

                                    try
                                    {
                                        await storage.CopyToDevCopiesAsync(
                                            resolvedFileInfo.FilePath
                                        );
                                    }
                                    catch { }
                                }
                                else
                                {
                                    var probe = await inspector.ProbeAsync(fullPath);
                                    if (
                                        (
                                            resolvedFileInfo.Type == MediaType.Audio
                                            && (
                                                probe.BitrateKbps is null || probe.BitrateKbps < 128
                                            )
                                        )
                                        || (
                                            resolvedFileInfo.Type == MediaType.Video
                                            && (
                                                probe.BitrateKbps is null || probe.BitrateKbps < 128
                                            )
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
                                    resolvedFileInfo.FilePath
                                );
                            }

                            try
                            {
                                await storage.CopyToDevCopiesAsync(resolvedFileInfo.FilePath);
                            }
                            catch (Exception copyEx)
                            {
                                logger.LogDebug(
                                    copyEx,
                                    "Не удалось синхронизировать dev-копию файла {File}",
                                    resolvedFileInfo.FilePath
                                );
                            }

                            var oldPath = existingAlert.FileInfo.FilePath ?? string.Empty;
                            if (
                                !string.IsNullOrWhiteSpace(oldPath)
                                && !oldPath.StartsWith(
                                    "memory/",
                                    StringComparison.OrdinalIgnoreCase
                                )
                                && oldPath.StartsWith("/", StringComparison.Ordinal)
                            )
                            {
                                var oldFullPath = ResolveMediaPath(oldPath);

                                if (System.IO.File.Exists(oldFullPath))
                                {
                                    System.IO.File.Delete(oldFullPath);
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
                                && !string.Equals(
                                    oldPath,
                                    newPath,
                                    StringComparison.OrdinalIgnoreCase
                                )
                                && !oldPath.StartsWith(
                                    "memory/",
                                    StringComparison.OrdinalIgnoreCase
                                )
                                && !newPath.StartsWith(
                                    "memory/",
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            {
                                // Аудит: источник искался перебором CWD/AppContext.BaseDirectory
                                // с приклейкой "wwwroot". Это давало разные ответы в
                                // зависимости от того, откуда запущен процесс, и не
                                // находило файл, если WebRootPath отличался.
                                var oldFullPath = ResolveMediaPath(oldPath);

                                if (!System.IO.File.Exists(oldFullPath))
                                {
                                    result = Ok(
                                        OperationResult<ApiMediaInfo?>.Fail(
                                            "Файл для перемещения не найден"
                                        )
                                    );
                                    return result;
                                }

                                var newFullPath = ResolveMediaPath(newPath);

                                var newDirectory = Path.GetDirectoryName(newFullPath);
                                if (!string.IsNullOrWhiteSpace(newDirectory))
                                {
                                    Directory.CreateDirectory(newDirectory);
                                }

                                System.IO.File.Move(oldFullPath, newFullPath, true);
                            }
                        }

                        var updatedAlert = CreateStoredAlert(alert, resolvedFileInfo);

                        dbContext.Entry(existingAlert).State = EntityState.Detached;
                        dbContext.Alerts.Update(updatedAlert);
                        await dbContext.SaveChangesAsync();

                        result = Ok(
                            OperationResult<ApiMediaInfo?>.Ok(new ApiMediaInfo(updatedAlert))
                        );
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении алерта {Id}", id);
            result = Ok(OperationResult<ApiMediaInfo?>.Fail("Ошибка при обновлении алерта"));
        }

        return result;
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

    private static MediaInfo CreateStoredAlert(ApiMediaInfo source, MediaFileInfo fileInfo)
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

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<OperationResult>> DeleteAlert(Guid id)
    {
        ActionResult<OperationResult> result = null!;

        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();

            var alert = await dbContext.Alerts.FirstOrDefaultAsync(a => a.Id == id);
            if (alert == null)
            {
                result = Ok(OperationResult.Fail($"Алерт с ID '{id}' не найден"));
            }
            else
            {
                dbContext.Alerts.Remove(alert);
                await dbContext.SaveChangesAsync();
                result = Ok(OperationResult.Ok());
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении алерта {Id}", id);
            result = Ok(OperationResult.Fail("Ошибка при удалении алерта"));
        }

        return result;
    }

    private static bool IsFreezeRuleValid(MediaMetaInfo metaInfo)
    {
        return !metaInfo.IsFreezeRequired || metaInfo.Priority == MediaAlertPriority.High;
    }
}
