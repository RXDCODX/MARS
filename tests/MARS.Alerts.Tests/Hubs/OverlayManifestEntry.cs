using System.Text.Json.Serialization;

namespace MARS.Alerts.Tests.Hubs;

/// <summary>Одна запись манифеста: имя метода хаба и наличие аргумента.</summary>
internal sealed record OverlayManifestEntry(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("hasPayload")] bool HasPayload
);
