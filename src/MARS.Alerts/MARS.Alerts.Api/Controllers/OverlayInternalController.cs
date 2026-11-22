using System.Text.Json;
using System.Text.Json.Serialization;
using MARS.Alerts.Models.Overlay;
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

        // Активная ветка ищется только среди свойств-событий (маркер OverlayEventAbstract),
        // а не среди всех свойств конверта.
        var branch = typeof(TelegramusEvent)
            .GetProperties()
            .Where(p => typeof(OverlayEventAbstract).IsAssignableFrom(p.PropertyType))
            .Select(p => (Name: p.Name, Payload: (OverlayEventAbstract?)p.GetValue(notification)))
            .FirstOrDefault(b => b.Payload is not null);

        if (branch.Payload is null)
        {
            return;
        }

        var method = typeof(IAlertsHub).GetMethod(branch.Name);
        if (method is null)
        {
            throw new InvalidOperationException(
                $"У события {branch.Name} нет метода в контракте оверлейного хаба."
            );
        }

        var parameter = method.GetParameters().FirstOrDefault();
        if (parameter is not null && !parameter.ParameterType.IsInstanceOfType(branch.Payload))
        {
            throw new InvalidOperationException(
                $"Параметр метода {branch.Name} ждёт {parameter.ParameterType.Name}, " +
                $"а ветка несёт {branch.Payload.GetType().Name}."
            );
        }

        object?[]? arguments = method.GetParameters().Length == 0 ? null : new[] { branch.Payload };
        await (Task)method.Invoke(hubContext.Clients.All, arguments)!;
    }
}