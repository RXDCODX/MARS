using System.Text.Json;
using System.Text.Json.Serialization;

namespace MARS.Alerts.Tests.Hubs;

/// <summary>Одна запись манифеста: имя метода хаба и наличие аргумента.</summary>
internal sealed record OverlayManifestEntry(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("hasPayload")] bool HasPayload
);

/// <summary>Чтение встроенного манифеста тем же кодом, что и генератор.</summary>
internal static class OverlayManifestReader
{
    public static IReadOnlyList<OverlayManifestEntry> Read(Stream stream) =>
        JsonSerializer.Deserialize<List<OverlayManifestEntry>>(stream) ?? [];

    public static string Serialize(IEnumerable<OverlayManifestEntry> entries) =>
        JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
}
