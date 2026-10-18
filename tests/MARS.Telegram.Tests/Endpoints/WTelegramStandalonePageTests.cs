using System.Net;
using System.Text.Json;
using MARS.Telegram.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Telegram.Tests.Endpoints;

/// <summary>
/// Минимальная страница входа в учётную запись Telegram.
/// </summary>
/// <remarks>
/// Служба часто запущена отдельно, без сайта Gateway, и код подтверждения вводить
/// некуда: первый вход пользовательского аккаунта не завершается. Страница решает
/// это и должна быть доступна по любому неописанному адресу — с неё начинают с
/// опечатки в пути.
/// </remarks>
public class WTelegramStandalonePageTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();

        // Динамический порт с ListenLocalhost запрещён, поэтому адрес указывается явно:
        // иначе хост поднимется на «localhost:0» и не поймёт, на каком порту стоит.
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        _app = builder.Build();
        _app.MapWTelegramStandalonePage();

        await _app.StartAsync(TestContext.Current.CancellationToken);

        var address = _app.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();

        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _client.Dispose();

        await _app.StopAsync(CancellationToken.None);
        await _app.DisposeAsync();
    }

    /// <summary>
    /// Страница открывается по любому неописанному адресу: человек начинает с
    /// опечатки в пути и попадает на форму, а не в 404 без подсказки.
    /// </summary>
    [Theory]
    [InlineData("/")]
    [InlineData("/wtelegram")]
    [InlineData("/WTelegram-Client/typo")]
    [InlineData("/api/что-то-не-существует")]
    public async Task Страница_Открывается_По_Любому_НеОписанному_Адресу(string path)
    {
        var response = await _client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/html", response.Content.Headers.ContentType?.MediaType ?? string.Empty);

        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("MARS.Telegram", html);
        Assert.Contains("id=\"code\"", html);
    }

    /// <summary>
    /// Страница показывает, по какому адресу попали: иначе «куда я вообще
    /// открылся» остаётся загадкой, и опечатку приходится искать вручную.
    /// </summary>
    [Fact]
    public async Task Страница_Показывает_Запрошенный_Адрес()
    {
        var html = await GetAsync("/WTelegram-C Client/typo");

        Assert.Contains("location.pathname", html);
    }

    /// <summary>
    /// Форма есть и она рабочая: без поля ввода страница ничего не решает.
    /// </summary>
    [Fact]
    public async Task На_Странице_Есть_Поле_Кода_И_Кнопка()
    {
        var html = await GetAsync("/");

        Assert.Contains("id=\"code\"", html);
        Assert.Contains("id=\"send\"", html);
        Assert.Contains("id=\"relogin\"", html);
    }

    /// <summary>
    /// Страница ходит ровно по тем адресам, которые служба описала. Расхождение
    /// означало бы форму с мёртвыми кнопками: адрес в странице и маршрут
    /// разъедутся молча, и причина будет выглядеть как «Telegram не отвечает».
    /// </summary>
    [Theory]
    [InlineData(WTelegramStandalonePage.StatusApiPath)]
    [InlineData(WTelegramStandalonePage.CodeApiPath)]
    [InlineData(WTelegramStandalonePage.ReloginApiPath)]
    public async Task Страница_Обращается_По_Описанным_Адресам(string path)
    {
        var html = await GetAsync("/");

        Assert.Contains(JsonSerializer.Serialize(path), html);
    }

    /// <summary>
    /// Внешних ресурсов нет: за прокси CDN недоступен, и страница, тянущая стили
    /// извне, осталась бы без оформления.
    /// </summary>
    [Fact]
    public async Task Внешних_Ресурсов_Нет()
    {
        var html = await GetAsync("/");

        Assert.DoesNotContain("http://", html.Replace("http://localhost", string.Empty));
        Assert.DoesNotContain("https://", html);
    }

    /// <summary>
    /// Состояние и ошибки выводятся из ответа службы: страница обязана их читать,
    /// иначе «ждёт код» и «прокси недоступен» выглядели бы одинаково.
    /// </summary>
    [Fact]
    public async Task Страница_Читает_Ошибку_Из_Ответа()
    {
        var html = await GetAsync("/");

        Assert.Contains("errorDetails", html);
        Assert.Contains("isAwaitingCode", html);
    }

    private async Task<string> GetAsync(string path) =>
        await _client.GetStringAsync(path, TestContext.Current.CancellationToken);
}
