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
    [ProducesResponseType(
        typeof(OperationResult<MediaStorageBrowseResult>),
        StatusCodes.Status200OK
    )]
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

    /// <summary>
    /// Отдача файла хранилища по пути — аналог GET-объекта в S3.
    /// </summary>
    /// <remarks>
    /// Аудит Stage 3: без этого UI не мог показать превью: существующие точки
    /// отдачи работают только по идентификатору записи алерта или мема.
    /// Путь приходит от клиента, поэтому проверяется на выход за пределы
    /// хранилища, а содержимое корзины не отдаётся — удалённый файл не должен
    /// быть доступен по прямой ссылке.
    /// </remarks>
    [HttpGet("file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile([FromQuery] string path)
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

        if (TrashPathBuilder.IsUnderTrash(relative))
        {
            return NotFound("Файл удалён");
        }

        var full = Path.GetFullPath(
            Path.Combine(
                webHostEnvironment.WebRootPath,
                relative.Replace('/', Path.DirectorySeparatorChar)
            )
        );

        if (!System.IO.File.Exists(full))
        {
            return NotFound($"Файл не найден: {relative}");
        }

        await storage.MarkDownloadedAsync(relative, HttpContext.RequestAborted);

        return PhysicalFile(
            full,
            MediaPath.GetContentType(relative),
            MediaPath.GetFileName(relative),
            enableRangeProcessing: true
        );
    }
}
