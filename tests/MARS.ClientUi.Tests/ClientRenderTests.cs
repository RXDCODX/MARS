using Microsoft.Playwright;

namespace MARS.ClientUi.Tests;

/// <summary>
/// Рендер каждой страницы клиента без падения.
/// </summary>
/// <remarks>
/// <para>
/// Отдельный набор от <c>ClientRouteTests</c> намеренно, хотя маршруты те же самые.
/// Там проверяется «маршрут открылся»: раздача, наличие содержимого, ошибки в
/// консоли. Здесь — ровно одно: <b>не упал рендер</b>. Это разные вопросы, и
/// раньше ответ на второй был случайным.
/// </para>
/// <para>
/// Признак падения рендера — заглушка <c>ErrorBoundary</c>, а не запись в консоли.
/// Граница смонтирована в <c>main.tsx</c> поверх всего приложения, поэтому любое
/// исключение в рендере подменяет экран её заглушкой. До появления
/// <c>data-testid</c> единственным признаком была строка
/// <c>console.error("Uncaught error:", …)</c> из <c>componentDidCatch</c>: убери её —
/// и весь набор останется зелёным, ничего не проверяя. Проверка на саму заглушку
/// такой зависимости не имеет.
/// </para>
/// <para>
/// Положительная сторона признака закрыта модульным тестом
/// <c>ErrorBoundary.test.tsx</c>: он убеждается, что граница показывает заглушку
/// при броске. Без этого «зелёный» здесь означал бы «детектор не работает».
/// </para>
/// <para>
/// Один тест на все маршруты, а не <c>[Theory]</c> по маршрутам: страница и контекст
/// создаются один раз вместо шестидесяти четырёх. Навигация полной перезагрузкой
/// (<c>GotoAsync</c>) сбрасывает состояние сторов, поэтому утечка между маршрутами
/// исключена. При такой экономии весь обход укладывается в две с половиной минуты.
/// </para>
/// <para>
/// Границы ожидания: не-оверлейные страницы ждут полторы секунды, оверлейные —
/// три. Этого хватает на исключение при монтировании, в эффектах и на первом
/// приходе данных. Ошибки, завязанные на длинную анимационную шкалу (в
/// <c>/mikumikubeam</c> бегущая строка стартует на девятой секунде), в это окно не
/// попадают — это остаточный пробел, и он назван прямо, а не закрыт утверждением,
/// которое на самом деле ничего не проверяет.
/// </para>
/// </remarks>
[Collection(ClientUiCollection.Name)]
public class ClientRenderTests(ClientUiFixture fixture)
{
    /// <summary>Заглушка границы ошибок: её наличие означает упавший рендер.</summary>
    private const string ErrorBoundarySelector = "[data-testid='error-boundary']";

    private const int SiteSettleMs = 1_500;
    private const int ObsSettleMs = 3_000;

    [Fact]
    public async Task Каждая_страница_открывается_без_падения_рендера()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var context = await fixture.NewContextAsync(cancellationToken);
        var page = await context.NewPageAsync();

        var failures = new List<string>();

        try
        {
            foreach (var route in ClientRoutes.All.Where(route => route.ShouldBeOpened))
            {
                var path = ResolvePath(route.Path);
                var settleMs = route.Type == "obs" ? ObsSettleMs : SiteSettleMs;

                if (await FellThroughBoundaryAsync(page, path, settleMs, cancellationToken))
                {
                    failures.Add(route.Path);
                }
            }
        }
        finally
        {
            await context.CloseAsync();
        }

        Assert.True(
            failures.Count == 0,
            "Рендер упал на маршрутах: "
                + string.Join(", ", failures)
                + ". На экране показана заглушка ErrorBoundary — приложение поймало"
                + " исключение при рендере и заменило экран заглушкой."
        );
    }

    /// <summary>
    /// Открывает маршрут и сообщает, показалась ли заглушка границы ошибок.
    /// </summary>
    private async Task<bool> FellThroughBoundaryAsync(
        IPage page,
        string path,
        int settleMs,
        CancellationToken cancellationToken
    )
    {
        await page.GotoAsync(
            // Относительный путь, а не склейка с `BaseUrl`: контексту задан
            // `BaseURL`, и первая склейка давала «http://localhost:9155environment-variables».
            path,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }
        );

        // Ждём, пока клиент смонтируется: до этого в корне ничего нет, и падение
        // рендера ещё просто не могло произойти.
        await page.WaitForFunctionAsync(
            "() => { const root = document.getElementById('root');"
                + " return root !== null && root.children.length > 0; }",
            null,
            new PageWaitForFunctionOptions { Timeout = 30_000 }
        );

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await page.WaitForSelectorAsync(
                ErrorBoundarySelector,
                new PageWaitForSelectorOptions
                {
                    State = WaitForSelectorState.Attached,
                    Timeout = settleMs,
                }
            );

            return true;
        }
        catch (TimeoutException)
        {
            // Заглушка не появилась за окно ожидания — ровно то, что нужно.
            return false;
        }
    }

    /// <summary>
    /// Подставляет параметры пути настоящими значениями.
    /// </summary>
    /// <remarks>
    /// <c>/media-info/edit/:id</c> — шаблон react-router, а не адрес: буквальное
    /// <c>:id</c> в строке запроса не совпадёт ни с одним маршрутом.
    /// </remarks>
    private static string ResolvePath(string pattern) =>
        string.Join(
            '/',
            pattern
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => segment.StartsWith(':') ? ClientRoute.ParameterValue : segment)
        );

    /// <summary>
    /// Набор рендера обязан обходить все страницы, а не часть.
    /// </summary>
    /// <remarks>
    /// Без этой проверки сужение условия <c>ShouldBeOpened</c> — например возврат
    /// исключения экранов OBS, как было до раунда пятнадцать, — снова тихо сократило
    /// бы набор вдвое, и он остался бы зелёным. Прежняя проверка требовала лишь
    /// «больше двадцати маршрутов», что верно и для двадцати одного.
    /// <para>
    /// Исключён ровно корневой путь: на нём виден сам факт поднятия стенда, и
    /// проверяют его другие тесты.
    /// </para>
    /// </remarks>
    [Fact]
    public void Обходятся_все_маршруты_кроме_корневого()
    {
        var skipped = ClientRoutes.All.Where(route => !route.ShouldBeOpened).ToArray();

        Assert.True(
            skipped.Length == 1 && skipped[0].Path == "/",
            "Набор рендера обходит не все страницы: пропущены "
                + string.Join(", ", skipped.Select(route => route.Path))
                + ". Исключён может быть только корень."
        );
    }
}
