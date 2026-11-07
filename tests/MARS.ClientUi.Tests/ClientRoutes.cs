using System.Text.Json;
using Microsoft.Playwright;

namespace MARS.ClientUi.Tests;

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
