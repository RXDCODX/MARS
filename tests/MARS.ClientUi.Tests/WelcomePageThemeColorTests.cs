using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace MARS.ClientUi.Tests;

/// <summary>
/// Жёсткий регрессионный тест цвета каждого блока главной страницы.
/// </summary>
/// <remarks>
/// <para>
/// Сценарий: светлая тема → тёмная → «системная» (разрешается по ОС) → светлая,
/// плюс переключение стиля сайта при тёмной теме. После каждого шага снимается
/// computed-цвет каждого блока и сверяется с ожидаемым значением из таблицы.
/// До фикса рассинхрона писателей DOM-темы часть блоков оставалась старого
/// цвета, и этот тест ловил расхождение expected vs actual.
/// </para>
/// <para>
/// Ожидаемые цвета взяты из источников истины: <c>global.scss</c> (CSS-переменные
/// <c>--site-*</c>) и токенов тем в <c>themes/illustrationTheme.ts</c> /
/// <c>themes/modernThemes.ts</c>. Фактический цвет читается через
/// <c>getComputedStyle</c> в реальном Chromium, поэтому проверяются и CSS-переменные,
/// и antd css-in-js.
/// </para>
/// </remarks>
[Collection(ClientUiCollection.Name)]
public class WelcomePageThemeColorTests(ClientUiFixture fixture)
{
    private const string LightPageBg = "rgb(255, 255, 255)";
    private const string DarkPageBg = "rgb(33, 37, 41)";
    private const string LightNavbarBg = "rgb(248, 249, 250)";
    private const string DarkNavbarBg = "rgb(52, 58, 64)";
    private const string LightNavbarBorder = "rgb(222, 226, 230)";
    private const string DarkNavbarBorder = "rgb(73, 80, 87)";
    private const string LightCardBg = "rgb(255, 240, 246)";
    private const string DarkCardBg = "rgb(31, 31, 31)";
    private const string LightSuccessBg = "rgb(81, 207, 102)";
    private const string DarkSuccessBg = "rgb(115, 209, 61)";
    private const string LightErrorBg = "rgb(250, 82, 82)";
    private const string DarkErrorBg = "rgb(255, 107, 107)";

    // Nord-палитра (modernThemes.ts, профиль nord)
    private const string NordLightCardBg = "rgb(255, 255, 255)";
    private const string NordDarkCardBg = "rgb(59, 66, 82)";
    private const string NordLightSuccessBg = "rgb(143, 188, 187)";
    private const string NordDarkSuccessBg = "rgb(163, 190, 140)";

    private static readonly string[] StatCardIds =
    {
        "stat-cpu",
        "stat-memory-working",
        "stat-gc-heap",
        "stat-uptime",
        "stat-threads",
        "stat-services",
        "stat-danbooru-autopost",
        "stat-nsfwbooru-autopost",
        "stat-telegram-discord-bridge",
    };

    private static readonly string[] ConnectionCardIds =
    {
        "stat-eventsub",
        "stat-twitch-chat",
        "stat-audio-soundbar",
        "stat-audio-tts",
        "stat-punto-switcher",
        "stat-tts-filter",
        "stat-discord-tts-relay",
    };

    private async Task<IPage> OpenWelcomePageAsync(
        IBrowserContext context,
        CancellationToken cancellationToken
    )
    {
        var page = await context.NewPageAsync();

        // Чистый старт: без сохранённой темы/стиля, чтобы стенд отдал дефолты
        // (illustration + light). Иначе localStorage от прошлого прогона сломает ожидания.
        await page.AddInitScriptAsync("() => { try { localStorage.clear(); } catch (e) {} }");

        await page.GotoAsync(
            "/welcome",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }
        );

        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"stat-cpu\"]') !== null",
            null,
            new PageWaitForFunctionOptions { Timeout = 30_000 }
        );

        cancellationToken.ThrowIfCancellationRequested();
        return page;
    }

    private static async Task<string> ComputedAsync(IPage page, string testId, string property)
    {
        return await page.EvaluateAsync<string>(
            $"() => {{ const el = document.querySelector('[data-testid=\"{testId}\"]'); return el ? getComputedStyle(el).{property} : ''; }}"
        );
    }

    private static string ExpectedCardBg(bool dark) => dark ? DarkCardBg : LightCardBg;

    private static string ExpectedSuccessBg(bool dark, string style) =>
        style == "nord"
            ? (dark ? NordDarkSuccessBg : NordLightSuccessBg)
            : (dark ? DarkSuccessBg : LightSuccessBg);

    private async Task AssertThemeColorsAsync(
        IPage page,
        bool dark,
        string style,
        CancellationToken cancellationToken
    )
    {
        var failures = new List<string>();

        // Layout (page-welcome) — фон из --site-bg-primary.
        var pageBg = await ComputedAsync(page, "page-welcome", "backgroundColor");
        var expectedPageBg = dark ? DarkPageBg : LightPageBg;
        if (pageBg != expectedPageBg)
        {
            failures.Add(
                $"page-welcome backgroundColor: ожидалось {expectedPageBg}, факт {pageBg}"
            );
        }

        // Navbar — фон и нижняя граница из --site-*.
        var navbarBg = await ComputedAsync(page, "navbar", "backgroundColor");
        var expectedNavbarBg = dark ? DarkNavbarBg : LightNavbarBg;
        if (navbarBg != expectedNavbarBg)
        {
            failures.Add($"navbar backgroundColor: ожидалось {expectedNavbarBg}, факт {navbarBg}");
        }

        var navbarBorder = await ComputedAsync(page, "navbar", "borderBottomColor");
        var expectedNavbarBorder = dark ? DarkNavbarBorder : LightNavbarBorder;
        if (navbarBorder != expectedNavbarBorder)
        {
            failures.Add(
                $"navbar borderBottomColor: ожидалось {expectedNavbarBorder}, факт {navbarBorder}"
            );
        }

        // Stat-карточки — фон из токена темы (antd Card colorBgContainer).
        var expectedStatBg =
            style == "nord"
                ? (dark ? NordDarkCardBg : NordLightCardBg)
                : (dark ? DarkCardBg : LightCardBg);

        foreach (var id in StatCardIds)
        {
            var present = await page.EvaluateAsync<bool>(
                $"() => document.querySelector('[data-testid=\"{id}\"]') !== null"
            );
            if (!present)
            {
                continue;
            }

            var bg = await ComputedAsync(page, id, "backgroundColor");
            if (bg != expectedStatBg)
            {
                failures.Add($"{id} backgroundColor: ожидалось {expectedStatBg}, факт {bg}");
            }
        }

        // card-quick-links — тоже antd Card.
        var quickLinksPresent = await page.EvaluateAsync<bool>(
            "() => document.querySelector('[data-testid=\"card-quick-links\"]') !== null"
        );
        if (quickLinksPresent)
        {
            var quickBg = await ComputedAsync(page, "card-quick-links", "backgroundColor");
            if (quickBg != expectedStatBg)
            {
                failures.Add(
                    $"card-quick-links backgroundColor: ожидалось {expectedStatBg}, факт {quickBg}"
                );
            }
        }

        // card-wedding-anniversary — опциональный блок, проверяем только если есть.
        var weddingPresent = await page.EvaluateAsync<bool>(
            "() => document.querySelector('[data-testid=\"card-wedding-anniversary\"]') !== null"
        );
        if (weddingPresent)
        {
            var weddingBg = await ComputedAsync(
                page,
                "card-wedding-anniversary",
                "backgroundColor"
            );
            if (weddingBg != expectedStatBg)
            {
                failures.Add(
                    $"card-wedding-anniversary backgroundColor: ожидалось {expectedStatBg}, факт {weddingBg}"
                );
            }
        }

        // Connection-карточки — фон зависит от состояния подключения.
        foreach (var id in ConnectionCardIds)
        {
            var present = await page.EvaluateAsync<bool>(
                $"() => document.querySelector('[data-testid=\"{id}\"]') !== null"
            );
            if (!present)
            {
                continue;
            }

            // innerText недоступен через getComputedStyle — читаем через textContent.
            var content = await page.EvaluateAsync<string>(
                $"() => {{ const el = document.querySelector('[data-testid=\"{id}\"]'); return el ? (el.textContent || '') : ''; }}"
            );
            var connected =
                content.Contains("Подключено")
                || content.Contains("Включено")
                || content.Contains("Активно");

            var expected = connected
                ? ExpectedSuccessBg(dark, style)
                : (dark ? DarkErrorBg : LightErrorBg);

            var bg = await ComputedAsync(page, id, "backgroundColor");
            if (bg != expected)
            {
                failures.Add($"{id} backgroundColor: ожидалось {expected}, факт {bg}");
            }
        }

        Assert.True(
            failures.Count == 0,
            "Несовпадение цветов блоков главной страницы:\n" + string.Join("\n", failures)
        );
    }

    [Fact]
    public async Task Каждый_блок_главной_меняет_цвет_при_переключении_темы()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var context = await fixture.NewContextAsync(cancellationToken);

        try
        {
            var page = await OpenWelcomePageAsync(context, cancellationToken);

            // 1. Светлая тема (дефолт) — illustration.
            await AssertThemeColorsAsync(
                page,
                dark: false,
                style: "illustration",
                cancellationToken
            );

            // 2. Переключаем на тёмную.
            await page.ClickAsync("[data-testid='button-theme-toggle']");
            await page.WaitForTimeoutAsync(300);
            await AssertThemeColorsAsync(
                page,
                dark: true,
                style: "illustration",
                cancellationToken
            );

            // 3. Тёмная -> системная: при светлой ОС разрешается в светлую.
            //    Это и есть сценарий «обратно»: без фикса часть блоков осталась бы тёмной.
            await page.ClickAsync("[data-testid='button-theme-toggle']");
            await page.WaitForTimeoutAsync(300);
            await AssertThemeColorsAsync(
                page,
                dark: false,
                style: "illustration",
                cancellationToken
            );

            // 4. Системная -> светлая (явный выбор).
            await page.ClickAsync("[data-testid='button-theme-toggle']");
            await page.WaitForTimeoutAsync(300);
            await AssertThemeColorsAsync(
                page,
                dark: false,
                style: "illustration",
                cancellationToken
            );

            // 5. Регрессия рассинхрона: переключаем стиль при тёмной теме,
            //    затем возвращаем светлую и проверяем, что ВСЕ блоки согласованы.
            await page.ClickAsync("[data-testid='button-theme-toggle']"); // -> dark
            await page.WaitForTimeoutAsync(300);

            await page.ClickAsync("[data-testid='button-style-selector']");
            await page.WaitForTimeoutAsync(200);
            await page.GetByText("Nord").ClickAsync();
            await page.WaitForTimeoutAsync(500);

            await AssertThemeColorsAsync(page, dark: true, style: "nord", cancellationToken);

            await page.ClickAsync("[data-testid='button-theme-toggle']"); // dark -> system (OS light)
            await page.WaitForTimeoutAsync(300);
            await AssertThemeColorsAsync(page, dark: false, style: "nord", cancellationToken);
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task Системная_тема_следует_за_цветовой_схемой_ОС()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // OS тёмная: системная тема должна разрешиться в тёмную.
        {
            var context = await fixture.NewContextAsync(cancellationToken);
            // Playwright выставляет prefers-color-scheme через опции контекста,
            // поэтому создаём контекст с ColorScheme.Dark вручную.
            await context.CloseAsync();

            var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(
                new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-dev-shm-usage" },
                }
            );
            var darkContext = await browser.NewContextAsync(
                new BrowserNewContextOptions
                {
                    ColorScheme = ColorScheme.Dark,
                    BaseURL = fixture.BaseUrl,
                }
            );

            try
            {
                var page = await OpenWelcomePageAsync(darkContext, cancellationToken);

                // light -> dark -> system (две переключения)
                await page.ClickAsync("[data-testid='button-theme-toggle']");
                await page.WaitForTimeoutAsync(200);
                await page.ClickAsync("[data-testid='button-theme-toggle']");
                await page.WaitForTimeoutAsync(300);

                await AssertThemeColorsAsync(
                    page,
                    dark: true,
                    style: "illustration",
                    cancellationToken
                );
            }
            finally
            {
                await darkContext.CloseAsync();
                await browser.DisposeAsync();
                playwright.Dispose();
            }
        }

        // OS светлая: системная тема разрешается в светлую.
        {
            var context = await fixture.NewContextAsync(cancellationToken);
            await context.CloseAsync();

            var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(
                new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-dev-shm-usage" },
                }
            );
            var lightContext = await browser.NewContextAsync(
                new BrowserNewContextOptions
                {
                    ColorScheme = ColorScheme.Light,
                    BaseURL = fixture.BaseUrl,
                }
            );

            try
            {
                var page = await OpenWelcomePageAsync(lightContext, cancellationToken);

                await page.ClickAsync("[data-testid='button-theme-toggle']");
                await page.WaitForTimeoutAsync(200);
                await page.ClickAsync("[data-testid='button-theme-toggle']");
                await page.WaitForTimeoutAsync(300);

                await AssertThemeColorsAsync(
                    page,
                    dark: false,
                    style: "illustration",
                    cancellationToken
                );
            }
            finally
            {
                await lightContext.CloseAsync();
                await browser.DisposeAsync();
                playwright.Dispose();
            }
        }
    }
}
