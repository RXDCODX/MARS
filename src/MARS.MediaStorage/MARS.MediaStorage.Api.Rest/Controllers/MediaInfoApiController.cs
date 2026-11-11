using System.Text.Json;
using MARS.MediaStorage.Api.Rest.Models;
using MARS.MediaStorage.Application.Alerts;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MARS.MediaStorage.Api.Rest.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaInfoApiController(IAlertService alerts, ILogger<MediaInfoApiController> logger)
    : ControllerBase
{
    private static readonly JsonSerializerOptions FormJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    [HttpGet]
    public async Task<ActionResult<OperationResult<List<ApiMediaInfo>>>> GetAllAlerts()
    {
        ActionResult<OperationResult<List<ApiMediaInfo>>> result = null!;

        try
        {
            var stored = await alerts.GetAllAsync(HttpContext.RequestAborted);
            var apiAlerts = stored.Select(a => new ApiMediaInfo(a)).ToList();
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
            var alert = await alerts.GetAsync(id, HttpContext.RequestAborted);

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
            var lookup = await alerts.GetFileAsync(id, cancellationToken);

            if (lookup.Status == AlertFileLookupStatus.NotFound)
            {
                result = NotFound(lookup.Message);
            }
            else
            {
                // Аудит: файл целиком читался в память через ReadAllBytesAsync.
                // Для крупных видео это исчерпывало память процесса.
                result = PhysicalFile(
                    lookup.FullPath!,
                    lookup.ContentType!,
                    lookup.FileName!,
                    enableRangeProcessing: true
                );
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
                else
                {
                    var saveResult = await alerts.CreateAsync(
                        alert,
                        request.File,
                        HttpContext.RequestAborted
                    );

                    result = saveResult.Succeeded
                        ? Ok(OperationResult<ApiMediaInfo?>.Ok(new ApiMediaInfo(saveResult.Alert!)))
                        : Ok(OperationResult<ApiMediaInfo?>.Fail(saveResult.Error!));
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
                    var saveResult = await alerts.UpdateAsync(
                        id,
                        alert,
                        request.File,
                        HttpContext.RequestAborted
                    );

                    result = saveResult.Succeeded
                        ? Ok(OperationResult<ApiMediaInfo?>.Ok(new ApiMediaInfo(saveResult.Alert!)))
                        : Ok(OperationResult<ApiMediaInfo?>.Fail(saveResult.Error!));
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

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<OperationResult>> DeleteAlert(Guid id)
    {
        ActionResult<OperationResult> result = null!;

        try
        {
            var deleted = await alerts.DeleteAsync(id, HttpContext.RequestAborted);

            result = deleted
                ? Ok(OperationResult.Ok())
                : Ok(OperationResult.Fail($"Алерт с ID '{id}' не найден"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении алерта {Id}", id);
            result = Ok(OperationResult.Fail("Ошибка при удалении алерта"));
        }

        return result;
    }
}
