using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Kernel;
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

    private static Task<IRequest> ObserveOverlayNegotiationAsync(IPage page) =>
        page.WaitForRequestAsync(request =>
            request.Url.Contains("hubs/overlay/negotiate", StringComparison.Ordinal)
        );

    private static async Task WaitForOverlayConnectionAsync(
        Task<IRequest> negotiation,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Наблюдение создано до page.GotoAsync: negotiate уходит сразу при
            // монтировании оверлея, и подписка после навигации его теряла.
            await negotiation.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        }
        catch (TimeoutException)
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
            var negotiation = ObserveOverlayNegotiationAsync(page);

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

            await WaitForOverlayConnectionAsync(negotiation, cancellationToken);

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
            var negotiation = ObserveOverlayNegotiationAsync(page);

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

            await WaitForOverlayConnectionAsync(negotiation, cancellationToken);

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

    private static readonly byte[] TinyPng =
    [
        0x89,
        0x50,
        0x4E,
        0x47,
        0x0D,
        0x0A,
        0x1A,
        0x0A,
        0x00,
        0x00,
        0x00,
        0x0D,
        0x49,
        0x48,
        0x44,
        0x52,
        0x00,
        0x00,
        0x00,
        0x01,
        0x00,
        0x00,
        0x00,
        0x01,
        0x08,
        0x06,
        0x00,
        0x00,
        0x00,
        0x1F,
        0x15,
        0xC4,
        0x89,
        0x00,
        0x00,
        0x00,
        0x0A,
        0x49,
        0x44,
        0x41,
        0x54,
        0x78,
        0x9C,
        0x63,
        0x00,
        0x01,
        0x00,
        0x00,
        0x05,
        0x00,
        0x01,
        0x0D,
        0x0A,
        0x2D,
        0xB4,
        0x00,
        0x00,
        0x00,
        0x00,
        0x49,
        0x45,
        0x4E,
        0x44,
        0xAE,
        0x42,
        0x60,
        0x82,
    ];

    private static async Task<(Guid Id, string FilePath)> CreateMediaStorageAlertAsync(
        ClientUiFixture fixture,
        int? twitchPointsCost,
        CancellationToken cancellationToken
    )
    {
        var fileName = $"e2e-{Guid.NewGuid():N}.png";
        var relativePath = $"Alerts/uploaded_mems/{fileName}";
        var expectedText = "Pyro активировал e2e-файл";

        var alert = new MediaInfo
        {
            TextInfo = new MediaTextInfo
            {
                Text = expectedText,
                TextColor = "#FFFFFF",
                KeyWordsColor = "#00FF00",
                KeyWordSybmolDelimiter = '#',
            },
            FileInfo = new MediaFileInfo
            {
                Type = MediaType.Image,
                FilePath = relativePath,
                FileName = fileName,
                Extension = ".png",
                IsLocalFile = true,
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
                Duration = 5,
                Priority = MediaAlertPriority.Normal,
                Volume = 100,
                IsEnabled = true,
                TwitchPointsCost = twitchPointsCost ?? 0,
            },
            StylesInfo = new MediaStylesInfo { IsBorder = true },
        };

        var alertJson = JsonSerializer.Serialize(
            alert,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters = { new JsonStringEnumConverter() },
            }
        );

        using var http = new HttpClient { BaseAddress = new Uri(fixture.BaseUrl) };
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(alertJson), "AlertJson");
        var fileContent = new ByteArrayContent(TinyPng);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            "image/png"
        );
        form.Add(fileContent, "file", fileName);

        using var response = await http.PostAsync("api/MediaInfoApi", form, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.True(
            response.IsSuccessStatusCode,
            $"Не удалось создать алерт в MediaStorage: {(int)response.StatusCode} {body}"
        );

        using var doc = JsonDocument.Parse(body);
        var result = doc.RootElement.GetProperty("result");
        var id = Guid.Parse(result.GetProperty("id").GetString()!);
        var filePath = result.GetProperty("fileInfo").GetProperty("filePath").GetString()!;

        return (id, filePath);
    }

    private static async Task DeleteMediaStorageAlertAsync(
        ClientUiFixture fixture,
        Guid id,
        CancellationToken cancellationToken
    )
    {
        using var http = new HttpClient { BaseAddress = new Uri(fixture.BaseUrl) };
        await http.DeleteAsync($"api/MediaInfoApi/{id}", cancellationToken);
    }

    [Fact]
    public async Task MediaStorageFileRendersInPyroAlerts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (alertId, filePath) = await CreateMediaStorageAlertAsync(
            fixture,
            null,
            cancellationToken
        );

        try
        {
            var browserContext = await fixture.NewContextAsync(cancellationToken);
            IPage? page = null;
            try
            {
                page = await browserContext.NewPageAsync();
                var negotiation = ObserveOverlayNegotiationAsync(page);
                await page.GotoAsync(
                    "/pyroalerts",
                    new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }
                );
                await page.WaitForFunctionAsync(
                    "() => { const root = document.getElementById('root'); return root !== null && root.children.length > 0; }",
                    null,
                    new PageWaitForFunctionOptions { Timeout = 30_000 }
                );
                await WaitForOverlayConnectionAsync(negotiation, cancellationToken);

                await using var publisher = await RewardEventPublisher.StartAsync(
                    RabbitHost(),
                    5672,
                    RabbitUser(),
                    RabbitPassword(),
                    cancellationToken
                );

                var media = new MediaInfo
                {
                    Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    TextInfo = new MediaTextInfo
                    {
                        Text = "Pyro активировал e2e-файл",
                        TextColor = "#FFFFFF",
                        KeyWordsColor = "#00FF00",
                        KeyWordSybmolDelimiter = '#',
                    },
                    FileInfo = new MediaFileInfo
                    {
                        Type = MediaType.Image,
                        FilePath = filePath,
                        FileName = "e2e.png",
                        Extension = ".png",
                        IsLocalFile = true,
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
                };

                await publisher.PublishAsync(
                    new RewardRedeemedEvent
                    {
                        RewardId = "e2e-file",
                        RewardCode = "e2e_file_probe",
                        RewardTitle = "E2E file probe",
                        Cost = 0,
                        UserId = "123456789",
                        UserName = "Pyro",
                        UserInput = "e2e-файл",
                        RedeemedAt = DateTime.UtcNow,
                        User = new TwitchUser
                        {
                            TwitchId = "123456789",
                            UserLogin = "pyro",
                            DisplayName = "Pyro",
                            ChatColor = "#FF0000",
                        },
                        Media = media,
                    },
                    cancellationToken
                );

                await page.WaitForFunctionAsync(
                    "selector => { const img = document.querySelector(selector); "
                        + "return img !== null && img.complete && img.naturalWidth > 0; }",
                    "img",
                    new PageWaitForFunctionOptions { Timeout = 30_000 }
                );
            }
            finally
            {
                if (page is not null)
                {
                    await page.CloseAsync();
                }

                await browserContext.CloseAsync();
            }
        }
        finally
        {
            await DeleteMediaStorageAlertAsync(fixture, alertId, cancellationToken);
        }
    }

    [Fact]
    public async Task CostMatchedRewardRendersMediaStorageAlert()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (alertId, _) = await CreateMediaStorageAlertAsync(fixture, 182, cancellationToken);

        try
        {
            var browserContext = await fixture.NewContextAsync(cancellationToken);
            IPage? page = null;
            try
            {
                page = await browserContext.NewPageAsync();
                var negotiation = ObserveOverlayNegotiationAsync(page);
                await page.GotoAsync(
                    "/pyroalerts",
                    new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }
                );
                await page.WaitForFunctionAsync(
                    "() => { const root = document.getElementById('root'); return root !== null && root.children.length > 0; }",
                    null,
                    new PageWaitForFunctionOptions { Timeout = 30_000 }
                );
                await WaitForOverlayConnectionAsync(negotiation, cancellationToken);

                await using var publisher = await RewardEventPublisher.StartAsync(
                    RabbitHost(),
                    5672,
                    RabbitUser(),
                    RabbitPassword(),
                    cancellationToken
                );

                await publisher.PublishAsync(
                    new RewardRedeemedEvent
                    {
                        RewardId = "e2e-cost",
                        RewardTitle = "E2E cost probe",
                        Cost = 182,
                        UserId = "123456789",
                        UserName = "Pyro",
                        RedeemedAt = DateTime.UtcNow,
                        User = new TwitchUser
                        {
                            TwitchId = "123456789",
                            UserLogin = "pyro",
                            DisplayName = "Pyro",
                            ChatColor = "#FF0000",
                        },
                    },
                    cancellationToken
                );

                await page.WaitForFunctionAsync(
                    $"() => (document.body.textContent || '').includes({JsonSerializer.Serialize("Pyro активировал e2e-файл")})",
                    null,
                    new PageWaitForFunctionOptions { Timeout = 30_000 }
                );

                await page.WaitForFunctionAsync(
                    "selector => { const img = document.querySelector(selector); "
                        + "return img !== null && img.complete && img.naturalWidth > 0; }",
                    "img",
                    new PageWaitForFunctionOptions { Timeout = 30_000 }
                );
            }
            finally
            {
                if (page is not null)
                {
                    await page.CloseAsync();
                }

                await browserContext.CloseAsync();
            }
        }
        finally
        {
            await DeleteMediaStorageAlertAsync(fixture, alertId, cancellationToken);
        }
    }
}
