using Microsoft.Playwright;

namespace MARS.ClientUi.Tests;

/// <summary>
/// Навигация по маршрутам клиента в настоящем браузере.
/// </summary>
/// <remarks>
/// Проверяется развёрнутый стенд, а не сборка исходников: маршрут, который
/// открывается «наполовину» — без клиентского чанка, с ошибкой в консоли или
/// с неработающим хабом, — снаружи выглядит так же, как рабочий.
/// </remarks>
[Collection(ClientUiCollection.Name)]
public class ClientRouteTests(ClientUiFixture fixture)
{
    /// <summary>
    /// Каждый маршрут должен открыться без перенаправления на страницу ошибки.
    /// </summary>
    /// <remarks>
    /// Список берётся из <c>routes.generated.json</c>, который порождается из
    /// <c>allRoutes</c>. Прежний рукописный список держал 58 маршрутов при 68
    /// записях в коде, и проверял сам себя.
    /// </remarks>
    [Theory]
    [MemberData(nameof(RoutesToOpen))]
    public async Task Маршрут_открывается_без_ошибки(string path)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var context = await fixture.NewContextAsync(cancellationToken);

        try
        {
            var page = await context.NewPageAsync();
            var consoleErrors = new List<string>();
            var pageErrors = new List<string>();

            page.Console += (_, message) =>
            {
                if (message.Type == "error")
                {
                    consoleErrors.Add(message.Text);
                }
            };
            page.PageError += (_, error) => pageErrors.Add(error);

            // Ожидание не networkidle: приложение держит открытым SignalR-соединение,
            // поэтому сеть не «успокаивается» никогда и networkidle всегда
            // отваливается по таймауту — даже на исправном экране. Вместо него
            // ждём конкретный признак: корень клиента наполнен содержимым.
            var response = await page.GotoAsync(
                path,
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }
            );

            Assert.NotNull(response);

            var body = await page.WaitForFunctionAsync(
                "() => { const root = document.getElementById('root');"
                    + " return root !== null && root.children.length > 0; }",
                null,
                new PageWaitForFunctionOptions { Timeout = 30_000 }
            );

            Assert.NotNull(body);

            var title = await page.TitleAsync();

            // Страница ошибки выглядит как пустой или сломанный экран, и
            // утверждать, что маршрут открылся, было бы самообманом.
            var visibleText = await page.InnerTextAsync("body");

            Assert.DoesNotContain("Page Not Found", title, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("404", title, StringComparison.OrdinalIgnoreCase);
            Assert.False(
                string.IsNullOrWhiteSpace(visibleText),
                $"Маршрут {path} открылся пустой страницей"
            );

            Assert.True(
                pageErrors.Count == 0,
                $"Маршрут {path} упал с ошибкой в скрипте: {string.Join("; ", pageErrors)}"
            );
            Assert.True(
                consoleErrors.Count == 0,
                $"Маршрут {path} дал ошибки в консоли: {string.Join("; ", consoleErrors)}"
            );
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    /// <summary>
    /// Маршруты, которые навигационный тест открывает.
    /// </summary>
    public static TheoryData<string> RoutesToOpen()
    {
        var data = new TheoryData<string>();

        foreach (var route in ClientRoutes.All.Where(route => route.ShouldBeOpened))
        {
            data.Add(route.Path);
        }

        return data;
    }

    /// <summary>
    /// Список маршрутов не должен молчать.
    /// </summary>
    /// <remarks>
    /// Без этой проверки пустой или нечитаемый <c>routes.generated.json</c>
    /// дал бы тест-набор без единого маршрута: он зелёный и ничего не проверяет.
    /// </remarks>
    [Fact]
    public void Список_маршрутов_не_пуст()
    {
        Assert.True(
            ClientRoutes.All.Count > 20,
            $"В routes.generated.json всего {ClientRoutes.All.Count} маршрутов. "
                + "Порог взят с прежнего рукописного списка, который держал 58."
        );

        Assert.True(
            ClientRoutes.All.Any(route => route.ShouldBeOpened),
            "Ни один маршрут не помечен к открытию: тест прошёл бы, не проверив ничего."
        );
    }

    /// <summary>
    /// Пути уникальны: повтор означает, что один экран перекрыт другим.
    /// </summary>
    [Fact]
    public void Пути_маршрутов_уникальны()
    {
        var duplicates = ClientRoutes
            .All.GroupBy(route => route.Path, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Assert.True(
            duplicates.Length == 0,
            $"Маршруты объявлены дважды: {string.Join(", ", duplicates)}. "
                + "React Router берёт первое совпадение, второе недостижимо."
        );
    }
}
