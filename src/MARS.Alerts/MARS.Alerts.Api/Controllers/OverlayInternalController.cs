using System.Text.Json;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Platform.Infrastructure.Web.AspNetCore.Extensions;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Models.Overlay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Alerts.Api.Controllers;

[ApiController]
[Route("api/internal/overlay")]
[Authorize(Policy = ServiceAuthExtensions.PolicyName)]
public class OverlayInternalController(
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ILogger<OverlayInternalController> logger
) : ControllerBase
{
    [HttpPost("events")]
    public async Task<ActionResult<OperationResult<OverlayFireResult>>> Fire(
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken = default
    )
    {
        TelegramusEvent? notification;
        try
        {
            notification = JsonSerializer.Deserialize<TelegramusEvent>(
                payload.GetRawText(),
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                }
            );
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Событие оверлея не разобрано: некорректный JSON");
            return BadRequest(
                OperationResult<OverlayFireResult>.Fail(
                    $"Событие не разобрано: {exception.Message}"
                )
            );
        }

        if (notification is null)
        {
            return BadRequest(
                OperationResult<OverlayFireResult>.Fail("Событие не разобрано: пустое тело.")
            );
        }

        await OverlayEventDispatcher.DispatchAsync(hubContext, notification, cancellationToken);

        logger.LogInformation("Fire: событие {EventCase} разослано", notification.EventCase);

        return Ok(OperationResult<OverlayFireResult>.Ok(new OverlayFireResult { Subscribers = 1 }));
    }
}