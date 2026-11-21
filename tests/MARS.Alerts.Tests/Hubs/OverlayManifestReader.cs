using System.Text.Json;

namespace MARS.Alerts.Tests.Hubs;

/// <summary>Чтение встроенного манифеста тем же кодом, что и генератор.</summary>
internal static class OverlayManifestReader
{
    public static IReadOnlyList<OverlayManifestEntry> Read(Stream stream) =>
        JsonSerializer.Deserialize<List<OverlayManifestEntry>>(stream) ?? [];

    public static string Serialize(IEnumerable<OverlayManifestEntry> entries) =>
        JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
}
