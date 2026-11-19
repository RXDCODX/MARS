using System.Text.Json;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.Playwright;

namespace MARS.ClientUi.Tests;

/// <summary>
/// Сквозной рендер алерта: награда Twitch → MARS.Alerts → SignalR → браузер.
/// </summary>
/// <remarks>
/// <para>
/// В отличие от <c>ClientRenderTests</c> (страница не падает) и
/// <c>ClientHubTests</c> (рукопожатие хаба), здесь проверяется, что конкретное
/// сообщение с конкретным текстом доходит до DOM после реального события награды
/// на брокере. Это именно тот разрыв, который не ловили модульные тесты слоёв.
/// </para>
/// <para>
/// Событие вбрасывается в RabbitMQ тем же конвейером, что и
/// <c>RewardRedemptionPublisher</c> (Wolverine), поэтому проходит все слушатели
/// MARS.Alerts: медиа-награда поднимает один алерт, cost-matched ветка молчит,
/// а неизвестный код награды игнорируется.
/// </para>
/// </remarks>
[Collection(ClientUiCollection.Name)]
public class RewardAlertFlowTests(ClientUiFixture fixture)
{
    private const string ErrorBoundarySelector = "[data-testid='error-boundary']";

    private static string Marker() => "e2e-" + Guid.NewGuid().ToString("N")[..8];

    private static RewardRedeemedEvent BuildReward(string marker) =>
        new()
        {
            RewardId = "e2e-reward",
            RewardCode = "e2e_render_probe",
            RewardTitle = "E2E render probe",
            Cost = 100,
            UserId = "123456789",
            UserName = "Pyro",
            UserInput = marker,
            RedeemedAt = DateTime.UtcNow,
            User = new TwitchUser
            {
                TwitchId = "123456789",
                UserLogin = "pyro",
                DisplayName = "Pyro",
                ChatColor = "#FF0000",
            },
            Media = new MediaInfo
            {
                Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                TextInfo = new MediaTextInfo
                {
                    Text = "{user.name} активировал {user.text}",
                    TextColor = "#FFFFFF",
                    KeyWordsColor = "#00FF00",
                    KeyWordSybmolDelimiter = '#',
                },
                FileInfo = new MediaFileInfo
                {
                    Type = MediaType.Image,
                    FilePath = "https://example.com/e2e-alert.png",
                    FileName = "e2e-alert.png",
                    Extension = ".png",
                    IsLocalFile = false,
                },
                PositionInfo = new MediaPositionInfo
                {
                    Width = 400,
                    Height = 300,
                    IsProportion = true,
                    IsHorizontalCenter = true,
                    IsVerticallCenter = true,
                },
                MetaInfo = new MediaMetaInfo
                {
                    DisplayName = "Pyro",
                    Duration = 30,
                    Priority = MediaAlertPriority.Normal,
                    Volume = 100,
                },
                StylesInfo = new MediaStylesInfo { IsBorder = true },
            },
        };

    private static async Task WaitForOverlayConnectionAsync(
        IPage page,
        CancellationToken cancellationToken
    )
    {
        var negotiateRequests = new List<string>();

        void Handler(object? _, IRequest request)
        {
            if (request.Url.Contains("hubs/overlay/negotiate", StringComparison.Ordinal))
            {
                negotiateRequests.Add(request.Url);
            }
        }

        page.Request += Handler;

        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);

            while (negotiateRequests.Count == 0 && DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            }
        }
        finally
        {
            page.Request -= Handler;
        }

        if (negotiateRequests.Count == 0)
        {
            throw new InvalidOperationException(
                "Браузер не открыл соединение с hubs/overlay: алерт не придёт."
            );
        }

        // Небольшая пауза, чтобы соединение успело открыться после negotiate.
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    private static string RabbitHost() =>
        Environment.GetEnvironmentVariable("MARS_E2E_RABBITMQ_HOST") is { Length: > 0 } h
            ? h
            : "localhost";

    private static string RabbitUser() =>
        Environment.GetEnvironmentVariable("RABBITMQ_USER") is { Length: > 0 } u ? u : "mars";

    private static string RabbitPassword() =>
        Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") is { Length: > 0 } p ? p : "mars";

    [Fact]
    public async Task RewardAlertRendersExactTextInBrowser()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var marker = Marker();
        var expectedText = $"Pyro активировал {marker}";

        var context = await fixture.NewContextAsync(cancellationToken);
        IPage? page = null;

        try
        {
            page = await context.NewPageAsync();

            await page.GotoAsync(
                "/pyroalerts",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }
            );

            await page.WaitForFunctionAsync(
                "() => { const root = document.getElementById('root');"
                    + " return root !== null && root.children.length > 0; }",
                null,
                new PageWaitForFunctionOptions { Timeout = 30_000 }
            );

            await WaitForOverlayConnectionAsync(page, cancellationToken);

            await using var publisher = await RewardEventPublisher.StartAsync(
                RabbitHost(),
                5672,
                RabbitUser(),
                RabbitPassword(),
                cancellationToken
            );

            // Публикация + ожидание текста. Если листенер alerts ещё не подписался
            // на момент публикации, событие уходит в пустоту — делаем один повтор
            // через 3 секунды.
            var appeared = false;

            for (var attempt = 0; attempt < 2 && !appeared; attempt++)
            {
                if (attempt > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                }

                await publisher.PublishAsync(BuildReward(marker), cancellationToken);

                try
                {
                    await page.WaitForFunctionAsync(
                        $"() => (document.body.textContent || '').includes({JsonSerializer.Serialize(expectedText)})",
                        null,
                        new PageWaitForFunctionOptions { Timeout = 30_000 }
                    );
                    appeared = true;
                }
                catch (TimeoutException)
                {
                    // Повторим публикацию на следующей итерации.
                }
            }

            Assert.True(
                appeared,
                $"Алерт с текстом «{expectedText}» не появился в DOM за отведённое время. "
                    + "Событие награды не дошло от брокера до оверлея."
            );

            // На экране не должно быть заглушки ErrorBoundary.
            var boundaryCount = await page.Locator(ErrorBoundarySelector).CountAsync();
            Assert.True(boundaryCount == 0, "Рендер алерта упал: показана заглушка ErrorBoundary.");
        }
        finally
        {
            if (page is not null)
            {
                await page.CloseAsync();
            }

            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task RewardAlertIsRenderedOnce()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var marker = Marker();
        var expectedText = $"Pyro активировал {marker}";

        var context = await fixture.NewContextAsync(cancellationToken);
        IPage? page = null;

        try
        {
            page = await context.NewPageAsync();

            await page.GotoAsync(
                "/pyroalerts",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }
            );

            await page.WaitForFunctionAsync(
                "() => { const root = document.getElementById('root');"
                    + " return root !== null && root.children.length > 0; }",
                null,
                new PageWaitForFunctionOptions { Timeout = 30_000 }
            );

            await WaitForOverlayConnectionAsync(page, cancellationToken);

            await using var publisher = await RewardEventPublisher.StartAsync(
                RabbitHost(),
                5672,
                RabbitUser(),
                RabbitPassword(),
                cancellationToken
            );

            await publisher.PublishAsync(BuildReward(marker), cancellationToken);

            await page.WaitForFunctionAsync(
                $"() => (document.body.textContent || '').includes({JsonSerializer.Serialize(expectedText)})",
                null,
                new PageWaitForFunctionOptions { Timeout = 30_000 }
            );

            // Ровно одно вхождение: cost-matched ветка не должна дублировать показ.
            var occurrences = await page.EvaluateAsync<int>(
                "needle => (document.body.textContent || '').split(needle).length - 1",
                expectedText
            );

            Assert.True(
                occurrences == 1,
                $"Ожидалось одно вхождение «{expectedText}», фактически {occurrences}. "
                    + "Возможен двойной показ алерта."
            );
        }
        finally
        {
            if (page is not null)
            {
                await page.CloseAsync();
            }

            await context.CloseAsync();
        }
    }
}
