using System.Text.Json;
using System.Text.Json.Serialization;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Alerts.Presentation.Overlay.Hubs.Interfaces;
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

        await DispatchAsync(hubContext, notification, cancellationToken);

        logger.LogInformation("Fire: событие {EventCase} разослано", notification.EventCase);

        return Ok(OperationResult<OverlayFireResult>.Ok(new OverlayFireResult { Subscribers = 1 }));
    }

    private static async Task DispatchAsync(
        IHubContext<AlertsHub, IAlertsHub> hubContext,
        TelegramusEvent notification,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var caseName = notification.EventCase.ToString();
        if (string.Equals(caseName, "None", StringComparison.Ordinal))
        {
            return;
        }

        var method = typeof(IAlertsHub).GetMethod(caseName);
        if (method is null)
        {
            throw new InvalidOperationException(
                $"У события {caseName} нет метода в контракте оверлейного хаба."
            );
        }

        var property = typeof(TelegramusEvent).GetProperty(caseName);
        var payload = property?.GetValue(notification);

        object?[]? arguments = method.GetParameters().Length == 0 ? null : new[] { payload };
        await (Task)method.Invoke(hubContext.Clients.All, arguments)!;
    }
}
