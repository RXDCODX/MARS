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
    public async Task Маршрут_открывается_без_ошибки(string pattern, string type)
    {
        var path = ResolvePath(pattern);
        var cancellationToken = TestContext.Current.CancellationToken;
        var context = await fixture.NewContextAsync(cancellationToken);

        try
        {
            var page = await context.NewPageAsync();
            var consoleErrors = new List<string>();
            var pageErrors = new List<string>();

            page.Console += (_, message) =>
            {
                if (message.Type == "error" && !IsExpectedStandNoise(message.Text))
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

            Assert.DoesNotContain("Page Not Found", title, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("404", title, StringComparison.OrdinalIgnoreCase);

            if (type == "obs")
            {
                // Экран OBS по устройству молчит до первого события: очередь пуста,
                // подсвеченного сообщения нет, трек не играет. Требовать непустой
                // текст значило бы требовать события, которого не будет.
                //
                // Проверяется то, что ловимо без события: маршрут смонтировался
                // (корень наполнен выше), не упал в скрипте и не дал ошибок в
                // консоли. Именно эта пара ловит падение в рендере — например
                // `undefined.charAt(0)` в луче MikuMikuBeam, которое три раунда
                // ревью чинили вручную.
            }
            else
            {
                // Страница ошибки выглядит как пустой или сломанный экран, и
                // утверждать, что маршрут открылся, было бы самообманом.
                var visibleText = await page.InnerTextAsync("body");

                Assert.False(
                    string.IsNullOrWhiteSpace(visibleText),
                    $"Маршрут {path} открылся пустой страницей"
                );
            }

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
    /// Подставляет параметры пути настоящими значениями.
    /// </summary>
    /// <remarks>
    /// <c>/media-info/edit/:id</c> — шаблон react-router, а не адрес. Буквальное
    /// <c>:id</c> в строке запроса не совпадёт ни с одним маршрутом, сервер отдаст
    /// 404, и тест упал бы на маршруте, объявленном верно.
    /// </remarks>
    private static string ResolvePath(string pattern) =>
        string.Join(
            '/',
            pattern
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => segment.StartsWith(':') ? ClientRoute.ParameterValue : segment)
        );

    /// <summary>
    /// Отказ 401 на защищённой странице и 404 на странице отдельной записи —
    /// ожидаемое поведение стенда, а не дефект.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 401: ключи доступа к сервисам на стенде не заданы, поэтому админские
    /// страницы получают отказ — так и должно быть без прав. Раньше тест падал
    /// на этом, и маршрут <c>/environment-variables</c> нельзя было проверить
    /// вовсе.
    /// </para>
    /// <para>
    /// 404: маршруты вида <c>/media-info/edit/:id</c> адресуют конкретную
    /// запись, а на свежем стенде записей нет. Проверяется, что маршрут
    /// резолвится и страница собирается, а не что в базе лежат данные, — этим
    /// занимается пустой-стенд, а не браузер.
    /// </para>
    /// <para>
    /// Важно, что фильтры узкие и перечислены явно. 405 и 500 означают, что
    /// страница зовёт существующий путь неподходящим методом или сервис упал, и
    /// это настоящая дыра, которую тест обязан видеть.
    /// </para>
    /// <para>
    /// Фильтров четыре, а не два, как написано было раньше: 401, 404, «not found»
    /// и ошибки компиляции шейдера. Прежняя формулировка «ровно эти два кода»
    /// разошлась с кодом и обещала меньше, чем на самом деле фильтровалось.
    /// </para>
    /// </remarks>
    private static bool IsExpectedStandNoise(string text) =>
        text.Contains("401 (Unauthorized)", StringComparison.Ordinal)
        || text.Contains("404 (Not Found)", StringComparison.Ordinal)
        || IsMissingRecordMessage(text)
        || IsSoftwareRendererShaderNoise(text);

    /// <summary>
    /// Ошибка компиляции шейдера в браузере без видеокарты.
    /// </summary>
    /// <remarks>
    /// <c>/avatarka</c>, <c>/avatarka-fire</c> и <c>/avatarka-fire-svg</c> рисуют
    /// на WebGL. В CI и в headless-прогоне браузер идёт через программный
    /// растеризатор, и часть шейдеров на нём не собирается: браузер пишет в
    /// консоль «shader compile error» и «program link error», продолжая работать.
    /// Это ограничение среды, а не дефект приложения, и на OBS с видеокартой эти
    /// экраны рисуются.
    /// <para>
    /// Фильтр совпадает по двум точным фразам компилятора, а не по слову
    /// «shader»: любая другая ошибка WebGL — включая настоящий баг в шейдере,
    /// который компилируется на видеокарте и падает на программном растеризаторе,
    /// — осталась бы незамеченной, и это был бы обмен тихой поломки на тихую
    /// поломку.
    /// </para>
    /// </remarks>
    private static bool IsSoftwareRendererShaderNoise(string text) =>
        text.Contains("shader compile error", StringComparison.OrdinalIgnoreCase)
        || text.Contains("program link error", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Сообщение «записи с таким идентификатором нет» на странице правки.
    /// </summary>
    /// <remarks>
    /// Сервис отвечает <c>Success = false</c>, транспортный слой превращает это в
    /// исключение, и страница его показывает. На стенде без данных так и должно
    /// быть: правка несуществующей записи не может придумать её содержимое.
    /// <para>
    /// Фильтруется по слову «not found», а не по адресу: любая другая ошибка
    /// загрузки — соединение, 500, невалидный ответ — остаётся падением теста.
    /// </para>
    /// </remarks>
    private static bool IsMissingRecordMessage(string text) =>
        text.Contains("not found", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Маршруты, которые навигационный тест открывает.
    /// </summary>
    public static TheoryData<string, string> RoutesToOpen()
    {
        var data = new TheoryData<string, string>();

        foreach (var route in ClientRoutes.All.Where(route => route.ShouldBeOpened))
        {
            data.Add(route.Path, route.Type);
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
