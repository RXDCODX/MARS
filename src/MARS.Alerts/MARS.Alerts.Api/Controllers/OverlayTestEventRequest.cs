using System.Text.Json;

namespace MARS.Alerts.Api.Controllers;

/// <summary>
/// Тело запроса тестового эндпоинта оверлей-событий.
/// </summary>
public sealed class OverlayTestEventRequest
{
    public string EventName { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
}
