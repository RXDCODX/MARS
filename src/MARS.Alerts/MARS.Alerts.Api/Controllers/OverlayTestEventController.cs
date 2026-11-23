using System.Text.Json;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Api.Controllers;

/// <summary>
/// Тестовый эндпоинт: принимает имя события оверлея и JSON-payload,
/// рассылает через SignalR-хаб как настоящее событие.
/// Доступен только при OverlayDevTools:Enabled = true.
/// </summary>
[ApiController]
[Route("api/overlay-test")]
public class OverlayTestEventController(
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    IConfiguration configuration,
    ILogger<OverlayTestEventController> logger
) : ControllerBase
{
    private static readonly HashSet<string> ParameterlessEvents = new(StringComparer.Ordinal)
    {
        "Credits",
        "MichaelJackson",
        "PhonkEdit",
    };

    private static readonly HashSet<string> AllEvents = new(StringComparer.Ordinal)
    {
        "Alert",
        "Alerts",
        "WaifuRoll",
        "AddNewWaifu",
        "ShowCurrentWife",
        "MergeWaifu",
        "FumoFriday",
        "NewMessage",
        "DeleteMessage",
        "MakeScreenParticles",
        "MakeScreenEmojisParticles",
        "RandomMem",
        "Adhd",
        "GaoAlert",
        "Credits",
        "MichaelJackson",
        "MikuMonday",
        "MikuMikuBeam",
        "PhonkEdit",
        "TikTokEdit",
        "AllRefund",
        "FumoRoll",
        "FrogRoll",
        "MikuRoll",
        "AdhdConfig",
    };

    /// <summary>Имена событий, которые принимает эндпоинт (для сверки с манифестом).</summary>
    public static IReadOnlyCollection<string> SupportedEvents => AllEvents;

    [HttpPost("event")]
    public async Task<ActionResult<OperationResult<string>>> Post(
        [FromBody] OverlayTestEventRequest? request,
        CancellationToken cancellationToken
    )
    {
        if (!configuration.GetValue<bool>("OverlayDevTools:Enabled"))
        {
            return Ok(
                OperationResult<string>.Fail(
                    "OverlayDevTools выключен. Включите OverlayDevTools:Enabled в конфигурации сервиса."
                )
            );
        }

        if (request is null || string.IsNullOrWhiteSpace(request.EventName))
        {
            return Ok(OperationResult<string>.Fail("Не указано имя события."));
        }

        if (!AllEvents.Contains(request.EventName))
        {
            return Ok(OperationResult<string>.Fail($"Неизвестное событие '{request.EventName}'."));
        }

        try
        {
            var client = (IClientProxy)hubContext.Clients.All;
            if (ParameterlessEvents.Contains(request.EventName))
            {
                await client.SendCoreAsync(request.EventName, [], cancellationToken);
            }
            else
            {
                var payload = ParsePayload(request.PayloadJson);
                await client.SendCoreAsync(request.EventName, [payload], cancellationToken);
            }

            logger.LogInformation("Overlay test event sent: {EventName}", request.EventName);
            return Ok(OperationResult<string>.Ok(request.EventName));
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "Invalid overlay test payload for {EventName}",
                request.EventName
            );
            return Ok(OperationResult<string>.Fail($"Некорректный JSON payload: {ex.Message}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send overlay test event {EventName}", request.EventName);
            return Ok(OperationResult<string>.Fail($"Ошибка отправки: {ex.Message}"));
        }
    }

    private static object ParsePayload(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, object>();
        }

        var element = JsonSerializer.Deserialize<JsonElement>(json);
        if (element.ValueKind == JsonValueKind.Undefined || element.ValueKind == JsonValueKind.Null)
        {
            return new Dictionary<string, object>();
        }

        return element;
    }
}

public sealed class OverlayTestEventRequest
{
    public string EventName { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
}
