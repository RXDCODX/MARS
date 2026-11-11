using System.Text;
using MARS.MediaStorage.Api.Rest.Models;
using MARS.MediaStorage.Application.Git;
using MARS.MediaStorage.Application.Media;
using MARS.MediaStorage.Application.Storage;
using MARS.Shared.Contracts.Models;
using Microsoft.AspNetCore.Mvc;

namespace MARS.MediaStorage.Api.Rest.Controllers;

/// <summary>
/// Управление файловым хранилищем wwwroot: просмотр, индексация, массовые
/// перемещения и удаления с мягкой корзиной.
/// </summary>
/// <remarks>
/// Аудит Stage 3: хранилище было доступно только через отдельный эндпоинт по
/// ID алерта, поэтому ни даты загрузки, ни массовые операции, ни корзина не
/// были доступны вовсе.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class MediaStorageController(
    IMediaStorageService storage,
    IWebHostEnvironment webHostEnvironment,
    IMediaGitService gitService,
    IMediaStorageBrowserService? browser = null
) : ControllerBase
{
    [HttpGet("entries")]
    [ProducesResponseType(
        typeof(OperationResult<List<MediaStorageEntryDto>>),
        StatusCodes.Status200OK
    )]
    public async Task<ActionResult<OperationResult<List<MediaStorageEntryDto>>>> List(
        [FromQuery] bool includeDeleted = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var entries = await storage.ListAsync(includeDeleted, cancellationToken);
            var dtos = entries.Select(MediaStorageEntryDto.From).ToList();

            return Ok(OperationResult<List<MediaStorageEntryDto>>.Ok(dtos));
        }
        catch (Exception ex)
        {
            return Ok(OperationResult<List<MediaStorageEntryDto>>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Глобальный поиск по проиндексированным файлам с серверной пагинацией.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(
        typeof(OperationResult<MediaStorageSearchResultDto>),
        StatusCodes.Status200OK
    )]
    public async Task<ActionResult<OperationResult<MediaStorageSearchResultDto>>> Search(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var result = await storage.SearchAsync(
                q ?? string.Empty,
                page,
                pageSize,
                cancellationToken
            );
            var dto = new MediaStorageSearchResultDto(
                result.Entries.Select(MediaStorageEntryDto.From).ToArray(),
                result.Total,
                result.Page,
                result.PageSize
            );
            return Ok(OperationResult<MediaStorageSearchResultDto>.Ok(dto));
        }
        catch (Exception ex)
        {
            return Ok(OperationResult<MediaStorageSearchResultDto>.Fail(ex.Message));
        }
    }

    [HttpGet("git/status")]
    [ProducesResponseType(typeof(OperationResult<GitStatusResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<OperationResult<GitStatusResult>>> GitStatus(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var status = await gitService.GetStatusAsync(cancellationToken);

            return Ok(OperationResult<GitStatusResult>.Ok(status));
        }
        catch (Exception ex)
        {
            return Ok(OperationResult<GitStatusResult>.Fail(ex.Message));
        }
    }

    [HttpPost("index")]
    [ProducesResponseType(typeof(OperationResult<IndexResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<OperationResult<IndexResultDto>>> Index(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var result = await storage.IndexAsync(cancellationToken);

            return Ok(OperationResult<IndexResultDto>.Ok(new IndexResultDto(result)));
        }
        catch (Exception ex)
        {
            return Ok(OperationResult<IndexResultDto>.Fail(ex.Message));
        }
    }

    [HttpPost("delete")]
    [ProducesResponseType(typeof(OperationResult<BulkOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<OperationResult<BulkOperationResultDto>>> Delete(
        [FromBody] BulkStorageRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request.Ids.Count == 0)
        {
            return Ok(OperationResult<BulkOperationResultDto>.Fail("Не указаны файлы"));
        }

        try
        {
            var result = await storage.SoftDeleteAsync(
                request.Ids,
                request.DryRun,
                cancellationToken
            );

            return Ok(
                OperationResult<BulkOperationResultDto>.Ok(new BulkOperationResultDto(result))
            );
        }
        catch (Exception ex)
        {
            return Ok(OperationResult<BulkOperationResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Перемещает файл в корзину по относительному пути. Используется preview pane,
    /// в том числе для файлов, которые ещё не были проиндексированы.
    /// </summary>
    [HttpDelete("file")]
    [ProducesResponseType(typeof(OperationResult<BulkOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<OperationResult<BulkOperationResultDto>>> DeleteFile(
        [FromQuery] string? path,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(path) || !MediaPath.IsSafeRelative(path))
        {
            return BadRequest("Недопустимый путь файла.");
        }

        var relative = MediaPath.Normalize(path);
        if (!MediaPath.IsVisibleRelativePath(relative) || !TryResolveSafeFilePath(relative, out _))
        {
            return NotFound("Файл не найден или недоступен.");
        }

        try
        {
            // Индексация перед поиском ID делает path-based действие доступным
            // и для физического файла, который пользователь добавил извне.
            await storage.IndexAsync(cancellationToken);
            var parent = GetParentDirectory(relative);
            var entries = await storage.ListDirectoryAsync(parent, cancellationToken);
            var entry = entries.FirstOrDefault(item =>
                string.Equals(item.Path, relative, StringComparison.Ordinal)
            );
            if (entry is null)
            {
                return NotFound("Файл не найден в индексе хранилища.");
            }

            var result = await storage.SoftDeleteAsync([entry.Id], false, cancellationToken);
            if (result.Succeeded != 1)
            {
                return Ok(OperationResult<BulkOperationResultDto>.Fail(
                    result.Errors.FirstOrDefault() ?? "Не удалось переместить файл в корзину."
                ));
            }

            return Ok(OperationResult<BulkOperationResultDto>.Ok(new BulkOperationResultDto(result)));
        }
        catch (Exception ex)
        {
            return Ok(OperationResult<BulkOperationResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Заменяет содержимое файла, сохраняя его имя и каталог.
    /// Старая версия сначала уходит в корзину и восстанавливается, если загрузка
    /// новой версии не удалась.
    /// </summary>
    [HttpPost("replace")]
    [RequestSizeLimit(1024 * 1024 * 1024)]
    [ProducesResponseType(typeof(OperationResult<BulkOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<OperationResult<BulkOperationResultDto>>> ReplaceFile(
        [FromForm] string? path,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(path) || !MediaPath.IsSafeRelative(path))
        {
            return BadRequest("Недопустимый путь файла.");
        }

        if (file is null || file.Length <= 0)
        {
            return BadRequest("Нужно выбрать непустой файл для замены.");
        }

        var relative = MediaPath.Normalize(path);
        if (!MediaPath.IsVisibleRelativePath(relative) || !TryResolveSafeFilePath(relative, out _))
        {
            return NotFound("Исходный файл не найден или недоступен.");
        }

        try
        {
            await storage.IndexAsync(cancellationToken);
            var parent = GetParentDirectory(relative);
            var entries = await storage.ListDirectoryAsync(parent, cancellationToken);
            var existing = entries.FirstOrDefault(item =>
                string.Equals(item.Path, relative, StringComparison.Ordinal)
            );
            if (existing is null)
            {
                return NotFound("Исходный файл не найден в индексе хранилища.");
            }

            var deleted = await storage.SoftDeleteAsync([existing.Id], false, cancellationToken);
            if (deleted.Succeeded != 1)
            {
                return Ok(OperationResult<BulkOperationResultDto>.Fail(
                    deleted.Errors.FirstOrDefault() ?? "Не удалось сохранить старую версию файла."
                ));
            }

            BulkOperationResult uploaded;
            try
            {
                using var replacement = new MediaUploadFile(
                    Path.GetFileName(relative),
                    file.OpenReadStream(),
                    file.ContentType,
                    file.Length
                );
                uploaded = await storage.UploadAsync([replacement], parent, cancellationToken);
            }
            catch
            {
                await storage.RestoreAsync([existing.Id], false, cancellationToken);
                throw;
            }

            if (uploaded.Succeeded == 1)
            {
                return Ok(OperationResult<BulkOperationResultDto>.Ok(new BulkOperationResultDto(uploaded)));
            }

            // Возвращаем исходный файл на прежний путь при отказе новой загрузки.
            await storage.RestoreAsync([existing.Id], false, cancellationToken);
            var reason = uploaded.Errors.FirstOrDefault() ?? "Новая версия не была загружена.";
            return Ok(OperationResult<BulkOperationResultDto>.Fail(reason));
        }
        catch (Exception ex)
        {
            return Ok(OperationResult<BulkOperationResultDto>.Fail(ex.Message));
        }
    }

    private static string GetParentDirectory(string relativePath)
    {
        var separatorIndex = relativePath.LastIndexOf('/');
        return separatorIndex < 0 ? string.Empty : relativePath[..separatorIndex];
    }

    [HttpPost("restore")]
    [ProducesResponseType(typeof(OperationResult<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<OperationResult<int>>> Restore(
        [FromBody] BulkRestoreRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request.Ids.Count == 0)
        {
            return Ok(OperationResult<int>.Fail("Не указаны файлы"));
        }

        try
        {
            var restored = await storage.RestoreAsync(
                request.Ids,
                request.DryRun,
                cancellationToken
            );

            return Ok(OperationResult<int>.Ok(restored));
        }
        catch (Exception ex)
        {
            return Ok(OperationResult<int>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Загрузка файлов в хранилище.
    /// </summary>
    /// <remarks>
    /// Аудит Stage 3: в UI не было загрузки — хранилище нельзя было наполнить.
    /// Запрос принимает несколько файлов сразу, потому что лимит тела запроса
    /// общий, а коммит всё равно делается один на всю пачку.
    /// </remarks>
    [HttpPost("upload")]
    [RequestSizeLimit(1024 * 1024 * 1024)]
    [ProducesResponseType(typeof(OperationResult<BulkOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<OperationResult<BulkOperationResultDto>>> Upload(
        [FromForm] IFormFileCollection? files,
        [FromForm] string? targetDirectory,
        CancellationToken cancellationToken = default
    )
    {
        if (files is null || files.Count == 0)
        {
            return Ok(OperationResult<BulkOperationResultDto>.Fail("Файлы не переданы"));
        }

        var uploads = new List<MediaUploadFile>(files.Count);
        var skippedEmpty = new List<string>();

        try
        {
            foreach (var file in files)
            {
                if (file.Length == 0)
                {
                    // Пустой файл не передаём сервису, но и не прячем: иначе
                    // клиент видел бы «запрошено 0, обработано 0» и думал,
                    // что запрос просто ничего не сделал.
                    skippedEmpty.Add($"{file.FileName}: файл пустой");
                    continue;
                }

                // Поток из формы, а не MemoryStream: файл читается на лету и
                // сразу пишется на диск с подсчётом хеша. Копирование в память
                // ради файла на десятки мегабайт исчерпывало бы память процесса.
                uploads.Add(
                    new MediaUploadFile(
                        file.FileName,
                        file.OpenReadStream(),
                        file.ContentType,
                        file.Length
                    )
                );
            }

            var result = await storage.UploadAsync(
                uploads,
                string.IsNullOrWhiteSpace(targetDirectory) ? "Uploads" : targetDirectory,
                cancellationToken
            );

            // Пустые файлы в счётчик не попали, поэтому возвращаем реальное
            // число переданных файлов и объединённый список причин.
            var merged = result with
            {
                Requested = files.Count,
                Failed = result.Failed + skippedEmpty.Count,
                Errors = [.. result.Errors, .. skippedEmpty],
            };

            return Ok(
                OperationResult<BulkOperationResultDto>.Ok(new BulkOperationResultDto(merged))
            );
        }
        catch (Exception ex)
        {
            return Ok(OperationResult<BulkOperationResultDto>.Fail(ex.Message));
        }
        finally
        {
            foreach (var upload in uploads)
            {
                upload.Dispose();
            }
        }
    }

    [HttpPost("move")]
    [ProducesResponseType(typeof(OperationResult<BulkOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<OperationResult<BulkOperationResultDto>>> Move(
        [FromBody] BulkMoveRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request.Ids.Count == 0)
        {
            return Ok(OperationResult<BulkOperationResultDto>.Fail("Не указаны файлы"));
        }

        try
        {
            var result = await storage.MoveAsync(
                request.Ids,
                request.TargetDirectory,
                request.DryRun,
                cancellationToken
            );

            return Ok(
                OperationResult<BulkOperationResultDto>.Ok(new BulkOperationResultDto(result))
            );
        }
        catch (Exception ex)
        {
            return Ok(OperationResult<BulkOperationResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Создаёт коммит и отправляет в удалённый репозиторий только выбранные файлы.
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(OperationResult<MediaGitSyncResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<OperationResult<MediaGitSyncResultDto>>> SyncToGit(
        [FromBody] MediaGitSyncRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request.Ids is null || request.Ids.Count == 0)
        {
            return Ok(OperationResult<MediaGitSyncResultDto>.Fail("Не указаны файлы"));
        }

        try
        {
            var ids = request.Ids.Distinct().ToArray();
            var requested = new HashSet<Guid>(ids);
            var entries = await storage.ListAsync(
                includeDeleted: false,
                cancellationToken: cancellationToken
            );
            var selected = entries.Where(entry => requested.Contains(entry.Id)).ToArray();

            if (selected.Length != ids.Length)
            {
                return Ok(
                    OperationResult<MediaGitSyncResultDto>.Fail(
                        "Некоторые выбранные файлы не найдены или находятся в корзине"
                    )
                );
            }

            var root = Path.GetFullPath(webHostEnvironment.WebRootPath);
            var rootPrefix = Path.EndsInDirectorySeparator(root)
                ? root
                : root + Path.DirectorySeparatorChar;
            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            var paths = new List<string>(selected.Length);

            foreach (var entry in selected)
            {
                var relative = MediaPath.Normalize(entry.Path);

                if (!MediaPath.IsSafeRelative(relative))
                {
                    return Ok(
                        OperationResult<MediaGitSyncResultDto>.Fail(
                            $"Недопустимый путь файла: '{entry.Path}'"
                        )
                    );
                }

                var fullPath = Path.GetFullPath(
                    Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar))
                );

                if (
                    !fullPath.StartsWith(rootPrefix, comparison) || !System.IO.File.Exists(fullPath)
                )
                {
                    return Ok(
                        OperationResult<MediaGitSyncResultDto>.Fail(
                            $"Файл отсутствует в хранилище: '{relative}'"
                        )
                    );
                }

                paths.Add(relative);
            }

            var result = await gitService.SyncAsync(
                $"Ручная синхронизация медиа: {paths.Count} файл(ов)",
                cancellationToken: cancellationToken,
                paths: paths
            );

            if (!result.Attempted)
            {
                return Ok(
                    OperationResult<MediaGitSyncResultDto>.Fail(
                        "Git-синхронизация отключена (MEDIA_GIT_ENABLED=false)"
                    )
                );
            }

            if (!result.Success)
            {
                return Ok(
                    OperationResult<MediaGitSyncResultDto>.Fail(
                        result.Error ?? "Не удалось синхронизировать выбранные файлы"
                    )
                );
            }

            return Ok(
                OperationResult<MediaGitSyncResultDto>.Ok(
                    new MediaGitSyncResultDto(paths.Count, result.Committed)
                )
            );
        }
        catch (Exception ex)
        {
            return Ok(OperationResult<MediaGitSyncResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Возвращает физические каталоги и файлы непосредственно внутри каталога.
    /// </summary>
    [HttpGet("browse")]
    [ProducesResponseType(typeof(OperationResult<MediaStorageBrowseResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResult<MediaStorageBrowseResult>>> Browse(
        [FromQuery] string? directory = null,
        CancellationToken cancellationToken = default
    )
    {
        if (browser is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        try
        {
            var result = await browser.BrowseAsync(directory, cancellationToken);
            return Ok(OperationResult<MediaStorageBrowseResult>.Ok(result));
        }
        catch (ArgumentException)
        {
            return BadRequest("Недопустимый путь каталога.");
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound("Каталог хранилища не найден.");
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound("Каталог хранилища не найден.");
        }
    }

    private const int TextPreviewLimitBytes = 512 * 1024;

    private static readonly IReadOnlyDictionary<string, string> PreviewContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".avif"] = "image/avif",
            [".mp4"] = "video/mp4",
            [".webm"] = "video/webm",
            [".ogv"] = "video/ogg",
            [".mov"] = "video/quicktime",
            [".avi"] = "video/x-msvideo",
            [".wmv"] = "video/x-ms-wmv",
            [".mp3"] = "audio/mpeg",
            [".wav"] = "audio/wav",
            [".ogg"] = "audio/ogg",
            [".opus"] = "audio/opus",
            [".flac"] = "audio/flac",
            [".pdf"] = "application/pdf",
        };

    private static readonly HashSet<string> TextPreviewExtensions = new(
        [".txt", ".log", ".json", ".xml", ".csv", ".md", ".markdown", ".yaml", ".yml"],
        StringComparer.OrdinalIgnoreCase
    );

    /// <summary>
    /// Inline-preview разрешён только для безопасных пассивных типов из allowlist.
    /// HTML, JavaScript и SVG остаются только скачиваемыми файлами.
    /// </summary>
    [HttpGet("preview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> Preview(
        [FromQuery] string? path,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest("Не указан путь");
        }

        if (!MediaPath.IsSafeRelative(path))
        {
            return BadRequest("Недопустимый путь");
        }

        var relative = MediaPath.Normalize(path);
        if (!MediaPath.IsVisibleRelativePath(relative))
        {
            return NotFound("Файл недоступен");
        }

        if (!TryResolveSafeFilePath(relative, out var fullPath))
        {
            return NotFound("Файл не найден или недоступен");
        }

        var extension = Path.GetExtension(relative);
        if (!PreviewContentTypes.TryGetValue(extension, out var contentType))
        {
            return StatusCode(
                StatusCodes.Status415UnsupportedMediaType,
                "Предпросмотр этого формата не поддерживается"
            );
        }

        try
        {
            var stream = OpenReadStream(fullPath);
            await storage.MarkDownloadedAsync(relative, cancellationToken);

            Response.Headers["Content-Disposition"] = "inline";
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return File(stream, contentType, enableRangeProcessing: true);
        }
        catch (FileNotFoundException)
        {
            return NotFound("Файл не найден");
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound("Файл не найден");
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound("Файл недоступен");
        }
    }

    /// <summary>
    /// Ограниченное текстовое представление для логов и текстовых документов.
    /// Содержимое возвращается как JSON-строка и никогда не вставляется как HTML.
    /// </summary>
    [HttpGet("text-preview")]
    [ProducesResponseType(
        typeof(OperationResult<MediaStorageTextPreviewDto>),
        StatusCodes.Status200OK
    )]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<OperationResult<MediaStorageTextPreviewDto>>> TextPreview(
        [FromQuery] string? path,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest("Не указан путь");
        }

        if (!MediaPath.IsSafeRelative(path))
        {
            return BadRequest("Недопустимый путь");
        }

        var relative = MediaPath.Normalize(path);
        if (!MediaPath.IsVisibleRelativePath(relative))
        {
            return NotFound("Файл недоступен");
        }

        if (!TextPreviewExtensions.Contains(Path.GetExtension(relative)))
        {
            return StatusCode(
                StatusCodes.Status415UnsupportedMediaType,
                "Текстовый предпросмотр этого формата не поддерживается"
            );
        }

        if (!TryResolveSafeFilePath(relative, out var fullPath))
        {
            return NotFound("Файл не найден или недоступен");
        }

        try
        {
            var fileInfo = new FileInfo(fullPath);
            var buffer = new byte[TextPreviewLimitBytes + 1];
            var bytesRead = 0;

            await using (var stream = OpenReadStream(fullPath))
            {
                while (bytesRead < buffer.Length)
                {
                    var read = await stream.ReadAsync(
                        buffer.AsMemory(bytesRead, buffer.Length - bytesRead),
                        cancellationToken
                    );
                    if (read == 0)
                    {
                        break;
                    }

                    bytesRead += read;
                }
            }

            var contentLength = Math.Min(bytesRead, TextPreviewLimitBytes);
            using var contentStream = new MemoryStream(buffer, 0, contentLength);
            using var reader = new StreamReader(
                contentStream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true
            );
            var content = await reader.ReadToEndAsync(cancellationToken);

            await storage.MarkDownloadedAsync(relative, cancellationToken);

            var result = new MediaStorageTextPreviewDto(
                content,
                fileInfo.Length,
                fileInfo.Length > TextPreviewLimitBytes || bytesRead > TextPreviewLimitBytes
            );
            return Ok(OperationResult<MediaStorageTextPreviewDto>.Ok(result));
        }
        catch (FileNotFoundException)
        {
            return NotFound("Файл не найден");
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound("Файл не найден");
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound("Файл недоступен");
        }
    }

    /// <summary>
    /// Скачивание файла с сохранением исторического attachment-контракта.
    /// Проверка каждого сегмента защищает и прямую загрузку от traversal/symlink.
    /// </summary>
    [HttpGet("file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(
        [FromQuery] string? path,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest("Не указан путь");
        }

        if (!MediaPath.IsSafeRelative(path))
        {
            return BadRequest("Недопустимый путь");
        }

        var relative = MediaPath.Normalize(path);
        if (!MediaPath.IsVisibleRelativePath(relative))
        {
            return NotFound("Файл удалён или недоступен");
        }

        if (!TryResolveSafeFilePath(relative, out var fullPath))
        {
            return NotFound("Файл не найден или недоступен");
        }

        try
        {
            var stream = OpenReadStream(fullPath);
            await storage.MarkDownloadedAsync(relative, cancellationToken);
            return File(
                stream,
                MediaPath.GetContentType(relative),
                MediaPath.GetFileName(relative),
                enableRangeProcessing: true
            );
        }
        catch (FileNotFoundException)
        {
            return NotFound("Файл не найден");
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound("Файл не найден");
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound("Файл недоступен");
        }
    }

    private bool TryResolveSafeFilePath(string relativePath, out string fullPath)
    {
        fullPath = string.Empty;

        try
        {
            if (
                string.IsNullOrWhiteSpace(webHostEnvironment.WebRootPath)
                || !MediaPath.IsSafeRelative(relativePath)
                || !MediaPath.IsVisibleRelativePath(relativePath)
            )
            {
                return false;
            }

            var root = Path.GetFullPath(webHostEnvironment.WebRootPath);
            if (!Directory.Exists(root) || IsReparsePoint(root))
            {
                return false;
            }

            var candidate = Path.GetFullPath(
                Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar))
            );
            var rootPrefix = Path.EndsInDirectorySeparator(root)
                ? root
                : root + Path.DirectorySeparatorChar;
            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            if (!candidate.StartsWith(rootPrefix, comparison))
            {
                return false;
            }

            var segments = relativePath.Split('/');
            var current = root;

            for (var index = 0; index < segments.Length; index++)
            {
                current = Path.Combine(current, segments[index]);
                if (IsReparsePoint(current))
                {
                    return false;
                }

                var isLast = index == segments.Length - 1;
                if (isLast ? !System.IO.File.Exists(current) : !Directory.Exists(current))
                {
                    return false;
                }
            }

            fullPath = candidate;
            return true;
        }
        catch (Exception ex) when (
            ex is ArgumentException
                or IOException
                or UnauthorizedAccessException
                or NotSupportedException
        )
        {
            return false;
        }
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return (System.IO.File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
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
    }

    private static FileStream OpenReadStream(string path) =>
        new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan
        );
}
