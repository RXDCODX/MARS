using System.Text.Json;
using Microsoft.Playwright;

namespace MARS.ClientUi.Tests;

/// <summary>
/// Описание маршрута, разобранное из <c>routes.generated.json</c>.
/// </summary>
/// <param name="Path">Путь, который открывает браузер.</param>
/// <param name="Type">Тип маршрута: <c>site</c>, <c>obs</c> и другие.</param>
/// <param name="Name">Название маршрута, если задано.</param>
public sealed record ClientRoute(string Path, string Type, string? Name)
{
    /// <summary>
    /// Значение, подставляемое в параметр пути вместо его имени.
    /// </summary>
    /// <remarks>
    /// Это настоящий GUID, а не «1», и разница принципиальна. Серверные
    /// маршруты объявлены как <c>{id:guid}</c>, и подстановка «1» не
    /// совпадала с шаблоном вовсе: маршрут не находился, и тест падал с 404 на
    /// маршруте, который был объявлен верно. Запись с таким идентификатором на
    /// стенде, конечно, не существует, но маршрут теперь резолвится — а это и
    /// проверяет навигационный тест.
    /// </remarks>
    public const string ParameterValue = "00000000-0000-0000-0000-000000000001";

    /// <summary>
    /// Открывать ли маршрут браузером.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Корневой путь не открывается: на нём сразу видно, что стенд поднялся, и
    /// остальные маршруты проверяют ту же раздачу клиента.
    /// </para>
    /// <para>
    /// Экраны OBS (<c>obs</c>) тоже открываются, и прежняя формулировка исключения
    /// была неверной в обеих частях. «Требуют window-обёртки, которой в обычном
    /// chromium нет» — неправда: <c>ClientHubTests</c> открывает <c>/waifu</c>, то
    /// есть obs-маршрут, в том же headless-браузере и успешно. «Их проверяет
    /// модульный vitest» — тоже: <c>OBSComponentsSmokeCoverage</c> только импортирует
    /// модули, а рендерят три экрана из тридцати одного. Падение в рендере
    /// (<c>undefined.charAt(0)</c> в луче MikuMikuBeam), ненашедшая хаб подписка или
    /// не пришедший на стенде чанк такой набор не ловит вовсе — а всё это чинилось
    /// три раунда ревью.
    /// </para>
    /// <para>
    /// Критерий «страница не пустая» для них другой, и это в
    /// <c>ClientRouteTests</c>: экран по устройству молчит до первого события.
    /// </para>
    /// </remarks>
    public bool ShouldBeOpened => Path != "/";
}

/// <summary>
/// Маршруты клиента, собранные при сборке SPA.
/// </summary>
/// <remarks>
/// Файл порождается тестом <c>src/tests/routesManifest.test.ts</c> из
/// <c>allRoutes</c> — того же массива, которым собирается роутер. Список не
/// пишется руками: прежний рукописный список держал 58 маршрутов при 68
/// записях в коде, и проверял сам себя.
/// </remarks>
public static class ClientRoutes
{
    private static readonly Lazy<IReadOnlyList<ClientRoute>> Cached = new(Load);

    /// <summary>Все маршруты клиента.</summary>
    public static IReadOnlyList<ClientRoute> All => Cached.Value;

    private static IReadOnlyList<ClientRoute> Load()
    {
        // Файл копируется в выходной каталог проектом; ищем рядом с сборкой,
        // а не по относительному пути от текущего каталога: при запуске из
        // контейнера рабочий каталог другой.
        var path = Path.Combine(AppContext.BaseDirectory, "routes.generated.json");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Не найден routes.generated.json: {path}. Файл порождается тестом "
                    + "src/tests/routesManifest.test.ts — запустите vitest перед сборкой тестов.",
                path
            );
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));

        if (
            !document.RootElement.TryGetProperty("routes", out var routes)
            || routes.ValueKind != JsonValueKind.Array
        )
        {
            throw new InvalidDataException(
                $"routes.generated.json не содержит массива routes: {path}"
            );
        }

        var result = new List<ClientRoute>();

        foreach (var element in routes.EnumerateArray())
        {
            var routePath = element.GetProperty("path").GetString() ?? "/";
            var type = element.TryGetProperty("type", out var typeElement)
                ? typeElement.GetString() ?? "site"
                : "site";
            var name = element.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString()
                : null;

            result.Add(new ClientRoute(routePath, type, name));
        }

        if (result.Count == 0)
        {
            throw new InvalidDataException(
                $"routes.generated.json пуст: {path}. Без маршрутов навигационный тест "
                    + "проверял бы только корень и проходил зелёным."
            );
        }

        return result;
    }
}
