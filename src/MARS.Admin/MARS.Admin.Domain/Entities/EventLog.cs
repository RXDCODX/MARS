using System.Text.Json;

namespace MARS.Admin.Domain.Entities;

/// <summary>
/// Запись в журнале бизнес-событий, потребляемых из RabbitMQ.
/// Используется панелью дашборда для живой ленты и исторических KPI.
/// </summary>
public class EventLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Тип события (имя контракта, например "WaifuRollEvent").</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Сервис-источник события (по имени контракта/очереди).</summary>
    public string SourceService { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    public string? ActorDisplayName { get; set; }

    public string PayloadJson { get; set; } = "{}";

    public static string SerializePayload(object? payload) =>
        payload is null ? "{}" : JsonSerializer.Serialize(payload);
}
