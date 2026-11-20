using System;
using MARS.MediaStorage.Application.MemoryStorage;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MARS.MediaStorage.Api.Rest.Controllers;

/// <summary>
/// Внутренние межсервисные эндпоинты MARS.MediaStorage.
/// </summary>
/// <remarks>
/// Доступны только с корректным <c>X-Api-Key</c> (политика <c>ServiceApiKey</c>):
/// браузер сюда не ходит. Хранилищем медиа владеет этот сервис, а остальные
/// кладут файлы для оверлея его эндпоинтом, а не держат собственную память.
/// </remarks>
[ApiController]
[Route("api/internal/media")]
[Authorize(Policy = "ServiceApiKey")]
public class MediaInternalController : ControllerBase
{
    /// <summary>
    /// Кладёт файл во внутреннее memory-хранилище и возвращает путь,
    /// по которому оверлей его загрузит.
    /// </summary>
    [HttpPost("memory-files")]
    public async Task<ActionResult<MemoryFileUploadResult>> UploadMemoryFile(
        [FromBody] MemoryFileUploadRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            return BadRequest("Имя файла не может быть пустым.");
        }

        byte[] content;
        try
        {
            content = Convert.FromBase64String(request.ContentBase64 ?? string.Empty);
        }
        catch (FormatException)
        {
            return BadRequest("Содержимое файла не в формате base64.");
        }

        if (content.Length == 0)
        {
            return BadRequest("Файл пуст.");
        }

        // Ограничение размера мема (32 МБ) — защита от случайных гигантских
        // загрузок через внутренний канал. Обычные мемы укладываются в пару МБ.
        const int maxBytes = 32 * 1024 * 1024;
        if (content.Length > maxBytes)
        {
            return BadRequest($"Файл слишком большой: {content.Length} байт (лимит {maxBytes}).");
        }

        var path = await MemoryStorage.AddFileAsync(request.FileName, content);
        return Ok(new MemoryFileUploadResult { Path = path });
    }
}
