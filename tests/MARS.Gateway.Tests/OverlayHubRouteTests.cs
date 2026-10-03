using System.Text.Json;

namespace MARS.Gateway.Tests;

/// <summary>
/// Проверки маршрутов YARP, от которых зависят оверлей и клиент.
/// </summary>
/// <remarks>
/// Опечатка в имени кластера или в пути не роняет сборку: YARP отдаёт 404, и
/// оверлей молча перестаёт получать алерты, а пользователь видит пустую
/// страницу. Единственная защита — тест, читающий тот же <c>appsettings.json</c>,
/// что и рантайм.
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

    /// <summary>
    /// Префиксы, которые обслуживает YARP. Каждый обязан иметь свой маршрут:
    /// иначе catch-all клиента отдаст <c>index.html</c> вместо ответа сервиса.
    /// </summary>
    private static readonly string[] YarpRoutedPrefixes =
    [
        "/api",
        "/hubs",
        "/storage-ui",
        "/memory",
    ];

    /// <summary>
    /// Эндпоинты самого Gateway из <c>UseMarsDefaults</c>. Их в YARP нет и не
    /// должно быть: это <c>MapHealthChecks("/health")</c> и
    /// <c>UseMetricServer("/metrics")</c>. Защищены они не порядком YARP, а
    /// приоритетом маршрутов ASP.NET Core, где сегмент-литерал побеждает
    /// catch-all. Проверять их наличие в <c>Yarp:Routes</c> бессмысленно.
    /// </summary>
    private static readonly string[] GatewayOwnEndpoints = ["/health", "/metrics"];

    /// <summary>
    /// <c>Order</c> catch-all маршрута клиента. YARP сортирует маршруты по
    /// возрастанию <c>Order</c>, затем по порядку в конфиге, и берёт первое
    /// совпадение, поэтому клиент обязан идти после всего остального.
    /// </summary>
    private const int SpaRouteOrder = 1000;

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
    /// Клиент раздаётся с корня, поэтому нужен catch-all. Без него Gateway на
    /// <c>/</c> отвечает 404, а 68 маршрутов SPA на react-router недоступны.
    /// </summary>
    [Fact]
    public void Spa_route_serves_everything_from_root()
    {
        using var settings = LoadGatewaySettings();

        Assert.True(
            Routes(settings).TryGetProperty("spa", out var route),
            "Маршрут spa не объявлен: клиент недоступен ни по одному адресу"
        );

        Assert.Equal("/{**remainder}", route.GetProperty("Match").GetProperty("Path").GetString());
        Assert.Equal("client-ui", route.GetProperty("ClusterId").GetString());
    }

    /// <summary>
    /// Порядок маршрутов — единственное, что удерживает catch-all от перехвата
    /// API. Без явного <c>Order</c> маршрут встанет по порядку в конфиге, и
    /// добавление любого нового маршрута после него молча сломает раздачу API:
    /// вместо ответа браузер получил бы <c>index.html</c> с кодом 200.
    /// </summary>
    [Fact]
    public void Spa_route_is_checked_after_every_other_route()
    {
        using var settings = LoadGatewaySettings();

        var spaOrder = Routes(settings).GetProperty("spa").GetProperty("Order").GetInt32();

        Assert.Equal(SpaRouteOrder, spaOrder);

        foreach (var route in Routes(settings).EnumerateObject())
        {
            if (route.Name == "spa")
            {
                continue;
            }

            var hasOrder = route.Value.TryGetProperty("Order", out var order);
            var routeOrder = hasOrder ? order.GetInt32() : 0;

            Assert.True(
                routeOrder < spaOrder,
                $"Маршрут '{route.Name}' имеет Order {routeOrder}, а spa — {spaOrder}. "
                    + "YARP возьмёт первый подходящий, и catch-all перехватит его путь."
            );
        }
    }

    /// <summary>
    /// Ни один из защищённых префиксов не должен пересекаться с путём хаба или с
    /// корнем клиента.
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

    /// <summary>
    /// Каждый префикс YARP должен быть покрыт своим маршрутом. Проверка ловит
    /// исчезновение маршрута из конфига: без неё catch-all тихо закрыл бы
    /// префикс, и API отвечало бы HTML вместо JSON.
    /// </summary>
    [Fact]
    public void Yarp_routed_prefixes_are_still_served_by_their_own_routes()
    {
        using var settings = LoadGatewaySettings();
        var paths = Routes(settings)
            .EnumerateObject()
            .Select(route => route.Value.GetProperty("Match").GetProperty("Path").GetString())
            .Where(path => path is not null)
            .ToArray();

        foreach (var prefix in YarpRoutedPrefixes)
        {
            var covered = paths.Any(path =>
                path!.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            );

            Assert.True(
                covered,
                $"Ни один маршрут не обслуживает '{prefix}'. Без него catch-all клиента "
                    + "отдаст index.html вместо ответа сервиса."
            );
        }
    }

    /// <summary>
    /// Отладочная отправка алертов вырезана вместе с контроллером
    /// <c>TestAlertsController</c>. Маршрут обязан исчезнуть вместе с ним: YARP
    /// не проверяет наличие цели, и оставшийся маршрут отвечал бы 502 на
    /// <c>/api/TestAlerts/*</c> вместо 404, то есть выглядел бы как живой.
    /// </summary>
    [Fact]
    public void Test_alerts_route_is_removed()
    {
        using var settings = LoadGatewaySettings();

        Assert.False(
            Routes(settings).TryGetProperty("test-alerts", out _),
            "Маршрут test-alerts остался после удаления TestAlertsController"
        );
    }

    /// <summary>
    /// Ни один маршрут не должен уводить на удалённый отладочный эндпоинт.
    /// </summary>
    [Fact]
    public void No_route_points_to_removed_test_alerts()
    {
        using var settings = LoadGatewaySettings();
        var paths = Routes(settings)
            .EnumerateObject()
            .Select(route => route.Value.GetProperty("Match").GetProperty("Path").GetString())
            .Where(path => path is not null)
            .ToArray();

        var leftovers = paths.Where(path =>
            path!.Contains("TestAlerts", StringComparison.OrdinalIgnoreCase)
        );

        Assert.Empty(leftovers);
    }

    /// <summary>
    /// Эндпоинты самого Gateway не должны попасть в YARP: если бы <c>/health</c>
    /// стал маршрутом прокси, compose-healthcheck начал бы опрашивать прокси
    /// вместо самого шлюза, а метрики перестали бы собираться.
    /// </summary>
    [Fact]
    public void Gateway_own_endpoints_are_not_proxied()
    {
        using var settings = LoadGatewaySettings();
        var paths = Routes(settings)
            .EnumerateObject()
            .Select(route => route.Value.GetProperty("Match").GetProperty("Path").GetString())
            .Where(path => path is not null)
            .ToArray();

        foreach (var endpoint in GatewayOwnEndpoints)
        {
            var proxied = paths.Any(path =>
                path!.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase)
            );

            Assert.False(
                proxied,
                $"'{endpoint}' — эндпоинт самого Gateway, но на него есть маршрут YARP: "
                    + "healthcheck и сбор метрик пошли бы через прокси."
            );
        }
    }
}
