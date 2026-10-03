using System.Text.Json;

namespace MARS.Gateway.Tests;

/// <summary>
/// Проверки маршрутов YARP, от которых зависит оверлей.
/// </summary>
/// <remarks>
/// Опечатка в имени кластера или в пути не роняет сборку: YARP отдаёт 404 на
/// <c>/hubs/overlay</c>, и оверлей молча перестаёт получать алерты. Единственная
/// защита — тест, читающий тот же <c>appsettings.json</c>, что и рантайм.
/// </remarks>
public class OverlayHubRouteTests
{
    private static JsonDocument LoadGatewaySettings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        Assert.True(File.Exists(path), $"Не найден appsettings.json: {path}");

        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static JsonElement Routes(JsonDocument settings) =>
        settings.RootElement.GetProperty("Yarp").GetProperty("Routes");

    private static JsonElement Clusters(JsonDocument settings) =>
        settings.RootElement.GetProperty("Yarp").GetProperty("Clusters");

    [Fact]
    public void Overlay_hub_route_exists()
    {
        using var settings = LoadGatewaySettings();

        Assert.True(
            Routes(settings).TryGetProperty("overlay-hub", out var route),
            "Маршрут overlay-hub не объявлен: оверлей получит 404 на /hubs/overlay"
        );

        Assert.Equal("/hubs/overlay", route.GetProperty("Match").GetProperty("Path").GetString());
    }

    /// <summary>
    /// Маршрут обязан указывать на существующий кластер. YARP проверяет это при
    /// старте, но падение пришлось бы на развёртывании, а не на сборке.
    /// </summary>
    [Fact]
    public void Overlay_hub_route_points_to_existing_cluster()
    {
        using var settings = LoadGatewaySettings();

        var clusterId = Routes(settings)
            .GetProperty("overlay-hub")
            .GetProperty("ClusterId")
            .GetString();

        Assert.True(
            Clusters(settings).TryGetProperty(clusterId!, out _),
            $"Кластер '{clusterId}' не объявлен в Yarp:Clusters"
        );
    }

    /// <summary>
    /// Путь хаба не должен попадать под префиксы API: иначе маршрут перехватит
    /// запрос раньше, чем дойдёт до оверлея.
    /// </summary>
    [Theory]
    [InlineData("/api/Telegram/")]
    [InlineData("/api/Obs/")]
    [InlineData("/api/MediaStorage/")]
    public void Overlay_hub_route_does_not_collide_with_api_prefixes(string apiPrefix)
    {
        var hubPath = "/hubs/overlay";

        Assert.False(
            apiPrefix.StartsWith(hubPath, StringComparison.OrdinalIgnoreCase)
                || hubPath.StartsWith(apiPrefix, StringComparison.OrdinalIgnoreCase),
            $"Путь хаба '{hubPath}' пересекается с API-префиксом '{apiPrefix}'"
        );
    }
}
