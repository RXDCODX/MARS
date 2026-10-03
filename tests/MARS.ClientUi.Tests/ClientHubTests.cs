using System.Text.Json;
using Microsoft.Playwright;

namespace MARS.ClientUi.Tests;

/// <summary>
/// Хабы объявляются в Gateway, но объявление маршрута ничего не говорит о том,
/// что по нему можно подключиться.
/// </summary>
/// <remarks>
/// <para>
/// Маршрут может остаться в <c>appsettings.json</c> и при этом вести в никуда:
/// сервис не поднял <c>MapHub</c>, путь отличается на регистр, или клиент шлёт
/// в <c>hubs/telegramus</c> то, что обслуживает <c>hubs/overlay</c>. Навигация
/// по маршруту такой случай не видит — страница открывается, хаб молча не
/// подключается, а первый алерт или трек не приходит никогда.
/// </para>
/// <para>
/// Проверяется рукопожатие, а не присутствие файла на диске: ответ 200 с
/// <c>connectionId</c> означает, что по пути стоит настоящий SignalR-хаб.
/// </para>
/// </remarks>
[Collection(ClientUiCollection.Name)]
public class ClientHubTests(ClientUiFixture fixture)
{
    /// <summary>
    /// Хабы, к которым обязан подключаться клиент, и путь, которым он идёт.
    /// </summary>
    /// <remarks>
    /// Пути написаны руками, а не взяты из кода клиента: если бы тест читал
    /// тот же адрес, что и приложение, он проверил бы, что строка равна самой
    /// себе. Ручной список — это утверждение о том, каким должен быть путь.
    /// </remarks>
    public static TheoryData<string> HubPaths =>
        new() { "hubs/overlay", "hubs/tuna", "hubs/scoreboard", "hubs/soundrequest" };

    [Theory]
    [MemberData(nameof(HubPaths))]
    public async Task Хаб_отвечает_на_рукопожатие(string hubPath)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        using var client = new HttpClient { BaseAddress = new Uri(fixture.BaseUrl) };

        var negotiate = $"{hubPath}/negotiate?negotiateVersion=1&connectionId=f";

        using var response = await client.PostAsync(negotiate, content: null, cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.True(
            response.IsSuccessStatusCode,
            $"Хаб {hubPath} не отдал рукопожатие: {(int)response.StatusCode}. Тело: {body}"
        );

        // connectionId в теле ответа — признак того, что ответил именно хаб.
        // Любой другой обработчик (Gateway, nginx, заглушка) вернул бы 200 и без
        // него, и проверка одного кода ответа была бы самообманом.
        using var document = JsonDocument.Parse(body);
        var hasConnectionId =
            document.RootElement.TryGetProperty("connectionId", out var connectionId)
            && !string.IsNullOrWhiteSpace(connectionId.GetString());

        Assert.True(
            hasConnectionId,
            $"Ответ на рукопожатие {hubPath} не содержит connectionId. Значит, по пути "
                + "отвечает не SignalR-хаб, и клиент не подключится. Тело: "
                + body
        );
    }

    /// <summary>
    /// Страница, которая монтирует оверлейную обёртку и потому открывает
    /// соединение.
    /// </summary>
    /// <remarks>
    /// Раньше проверка шла на <c>/</c>, и это было неверно: корень не
    /// монтирует <c>OBSComponentWrapper</c>, а именно он вызывает <c>start()</c>
    /// стора. На корне соединение и не должно открываться, так что тест
    /// сообщал о неисправности там, где её нет. Экран оверлея обёртку
    /// монтирует.
    /// </remarks>
    private const string OverlayPage = "/waifu";

    /// <summary>
    /// Оверлейный хаб должен доезжать из браузера, а не только отвечать на curl.
    /// </summary>
    /// <remarks>
    /// Рукопожатие выше проверяет маршрут; этот тест проверяет, что приложение
    /// само открывает соединение. Признак взят из сети, а не из глобальной
    /// переменной на <c>window</c>: такое свойство пришлось бы добавить в
    /// боевой код ради теста, и оно ничего не сказало бы о самом хабе.
    /// </remarks>
    [Fact]
    public async Task Оверлейный_хаб_подключается_из_браузера()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var context = await fixture.NewContextAsync(cancellationToken);

        try
        {
            var page = await context.NewPageAsync();
            var negotiateRequests = new List<string>();

            page.Request += (_, request) =>
            {
                if (request.Url.Contains("hubs/overlay/negotiate", StringComparison.Ordinal))
                {
                    negotiateRequests.Add(request.Url);
                }
            };

            await page.GotoAsync(
                OverlayPage,
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }
            );

            // Ждём не фиксированное время, а сам факт запроса: он приходит на
            // подъёме приложения, и ждать его — точнее, чем ждать паузу.
            var deadline = DateTime.UtcNow.AddSeconds(30);

            while (negotiateRequests.Count == 0 && DateTime.UtcNow < deadline)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            }

            Assert.True(
                negotiateRequests.Count > 0,
                $"Браузер не отправил negotiate к hubs/overlay со страницы {OverlayPage}. "
                    + "Приложение не открывает соединение с оверлейным хабом: первый алерт "
                    + "после старта не придёт."
            );
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
