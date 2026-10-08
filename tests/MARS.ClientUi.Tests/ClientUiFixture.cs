using Microsoft.Playwright;

namespace MARS.ClientUi.Tests;

/// <summary>
/// Браузер и стенд для навигационных тестов клиента.
/// </summary>
/// <remarks>
/// <para>
/// Стенд поднимает CI: <c>docker compose up -d --build --wait</c>, наружу
/// открыт только Gateway на 10155. Тесты ходят по нему, как это сделает
/// браузер зрителя.
/// </para>
/// <para>
/// Недоступный стенд — это провал, а не предупреждение. Иначе при
/// неподнявшемся стенде все тесты проходили бы зелёными, не проверив ни
/// одного маршрута: браузер не открывает страницу, ошибок в консоли нет,
/// утверждения не выполняются, а отчёт показывает успех. Именно так выглядела бы
/// задача, которая «проходит» при сломанном развёртывании.
/// </para>
/// </remarks>
public sealed class ClientUiFixture : IAsyncLifetime
{
    /// <summary>
    /// Адрес стенда. Переопределяется переменной окружения — в CI стенд
    /// слушает localhost через <c>--network host</c>.
    /// </summary>
    public const string BaseUrlVariable = "MARS_CLIENTUI_BASE_URL";

    /// <summary>
    /// Порт взят из <c>ports</c> в compose, а не выбран отдельно: стенд в CI
    /// поднимается тем же compose, и значение по умолчанию, разошедшееся с ним,
    /// дало бы зелёный прогон против пустоты или чужого процесса на том же порту.
    /// </summary>
    private const string DefaultBaseUrl = "http://localhost:10155";

    private readonly SemaphoreSlim _gate = new(1, 1);

    private IPlaywright? _playwright;

    private IBrowser? _browser;

    /// <summary>Адрес стенда, по которому идут тесты.</summary>
    public string BaseUrl =>
        Environment.GetEnvironmentVariable(BaseUrlVariable) is { Length: > 0 } configured
            ? configured
            : DefaultBaseUrl;

    public async ValueTask InitializeAsync()
    {
        await WaitForStandAsync(TestContext.Current.CancellationToken);

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions
            {
                // В контейнере CI chrome-sandbox не работает: sandbox требует
                // прав, которых у контейнера нет, и без этого флага браузер не
                // стартует вовсе.
                Args = ["--no-sandbox", "--disable-dev-shm-usage"],
                Headless = true,
            }
        );
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
            _browser = null;
        }

        // Playwright держит процесс драйвера: без освобождения контейнер CI
        // не завершится и упадёт по таймауту, а тесты будут зелёными.
        _playwright?.Dispose();
        _playwright = null;

        _gate.Dispose();
    }

    /// <summary>
    /// Открывает страницу. Браузер общий, а контекст на тест: состояние вкладки
    /// не должно переезжать между проверками.
    /// </summary>
    public async Task<IPage> OpenPageAsync(CancellationToken cancellationToken)
    {
        var context = await NewContextAsync(cancellationToken);

        return await context.NewPageAsync();
    }

    /// <summary>
    /// Новый контекст браузера с адресом стенда.
    /// </summary>
    /// <remarks>
    /// Открыт наружу, чтобы тест мог подписаться на события консоли до
    /// навигации: подписка после <c>page.GotoAsync</c> пропустила бы ошибки,
    /// возникшие при загрузке, — а именно они и означают сломанный чанк.
    /// </remarks>
    public async Task<IBrowserContext> NewContextAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (_browser is null)
            {
                throw new InvalidOperationException(
                    "Браузер не создан: фикстура не инициализирована."
                );
            }

            return await _browser.NewContextAsync(
                new BrowserNewContextOptions { BaseURL = BaseUrl }
            );
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Проверяет, что стенд отвечает, иначе падает с внятным текстом.
    /// </summary>
    private async Task WaitForStandAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        for (var attempt = 1; attempt <= 30; attempt++)
        {
            try
            {
                using var response = await client.GetAsync(BaseUrl, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Стенд ещё поднимается: Gateway не слушает или контейнер
                // клиента не готов. Пробуем снова.
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new InvalidOperationException(
            $"Стенд не отвечает по адресу {BaseUrl} минуту с лишним. Навигационные тесты "
                + "клиента не могут быть проведены. Поднимите стенд: "
                + "docker compose up -d --build --wait"
        );
    }
}

/// <summary>
/// Фикстура на весь набор: один браузер, один стенд.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ClientUiCollection : ICollectionFixture<ClientUiFixture>
{
    public const string Name = "client-ui";
}
