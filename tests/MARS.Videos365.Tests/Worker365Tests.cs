using System.Net;
using MARS.Shared.Configuration;
using MARS.Shared.Telegram;
using MARS.Videos365.Configuration;
using MARS.Videos365.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Videos365.Tests;

/// <summary>
/// Конвейер публикации видео.
///
/// Проверяется, что проход останавливается там, где это важно для зрителя: при
/// неполной конфигурации, при недоступном источнике и при пустом успешном ответе.
/// В каждом из этих случаев видео не должно помечаться загруженным, а
/// недоступность источника обязана уйти администраторам.
/// </summary>
public class Worker365Tests
{
    private static readonly Uri Site = new("https://example.test");

    private readonly Mock<ITelegramAdminMessenger> _messenger = new();

    /// <summary>
    /// Неполная конфигурация — обычное состояние стенда: конвейер не стартует вовсе,
    /// и это не ошибка.
    /// </summary>
    [Fact]
    public async Task IncompleteConfigSkipsTheRun()
    {
        var worker = Create(new Config365 { Site = Site.ToString() });

        await worker.Main();

        Assert.Empty(_messenger.Invocations);
    }

    /// <summary>
    /// Недоступный источник уведомляет администраторов и завершает проход: иначе
    /// видео молча помечались бы загруженными, которых в канале нет.
    /// </summary>
    [Fact]
    public async Task UnavailableSiteNotifiesAdmins()
    {
        var worker = Create(CompleteConfig(), dns: []);

        await worker.Main();

        _messenger.Verify(
            messenger =>
                messenger.SendAsync(
                    It.IsAny<long>(),
                    It.Is<string>(text => text.Contains("недоступен")),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Успешная проверка идёт дальше: источник опрашивается дважды — сначала
    /// проверкой доступности, затем самим обходом.
    /// </summary>
    [Fact]
    public async Task AvailableSiteReachesTheSource()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        var worker = Create(CompleteConfig(), handler: handler);

        await worker.Main();

        Assert.Equal(2, handler.Requests);
    }

    /// <summary>
    /// Источник отвечает ошибкой — это недоступность, а не исключение: проход
    /// завершается уведомлением, а не падением фоновой задачи.
    /// </summary>
    [Fact]
    public async Task ServerErrorIsTreatedAsUnavailable()
    {
        var worker = Create(
            CompleteConfig(),
            handler: new RecordingHandler(HttpStatusCode.InternalServerError)
        );

        await worker.Main();

        _messenger.Verify(
            messenger =>
                messenger.SendAsync(
                    It.IsAny<long>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Вне production конвейер не запускается: на стенде разработки источник
    /// трогать незачем.
    /// </summary>
    [Fact]
    public async Task OutsideProductionNothingRuns()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        var worker = Create(CompleteConfig(), handler: handler, isProduction: false);

        await worker.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, handler.Requests);
    }

    /// <summary>
    /// В production конвейер стартует, но падение обхода не роняет хост: ошибка
    /// попадает в журнал.
    /// </summary>
    [Fact]
    public async Task ProductionRunIsStarted()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        var worker = Create(CompleteConfig(), handler: handler, isProduction: true);

        await worker.StartAsync(TestContext.Current.CancellationToken);
        await worker.StopAsync(TestContext.Current.CancellationToken);

        await WaitUntilAsync(() => handler.Requests > 0);
    }

    private static Config365 CompleteConfig() =>
        new()
        {
            Site = Site.ToString(),
            Login = "login",
            Password = "password",
            TelegramChannelId = 42,
        };

    private Worker365 Create(
        Config365 config,
        IPAddress[]? dns = null,
        RecordingHandler? handler = null,
        bool isProduction = true
    ) =>
        new(
            Options.Create(config),
            Factory(handler ?? new RecordingHandler(HttpStatusCode.OK)),
            new FakeLifetime(),
            new FakeEnvironment(isProduction),
            new SiteAvailabilityChecker(
                new StubDns(dns ?? [IPAddress.Loopback]),
                Factory(handler ?? new RecordingHandler(HttpStatusCode.OK)),
                NullLogger<SiteAvailabilityChecker>.Instance
            ),
            new SiteUnavailableNotifier(
                _messenger.Object,
                Options.Create(new TelegramConfig { AdminIds = [123] }),
                NullLogger<SiteUnavailableNotifier>.Instance
            ),
            NullLogger<Worker365>.Instance
        );

    private static IHttpClientFactory Factory(HttpMessageHandler handler) =>
        new SingleHandlerFactory(handler);

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        Assert.Fail("Конвейер не дошёл до источника");
    }

    private sealed class SingleHandlerFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubDns(IPAddress[] addresses) : IDnsResolver
    {
        public Task<IPAddress[]> GetHostAddressesAsync(
            string hostNameOrAddress,
            CancellationToken cancellationToken
        ) => Task.FromResult(addresses);
    }

    private sealed class RecordingHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests++;

            var response = new HttpResponseMessage(status);
            if (status == HttpStatusCode.OK)
            {
                response.Headers.Add("Set-Cookie", "PHPSESSID=abc123; path=/");
            }

            return Task.FromResult(response);
        }
    }

    private sealed class FakeLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;

        public CancellationToken ApplicationStopping => CancellationToken.None;

        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication() { }
    }

    private sealed class FakeEnvironment(bool isProduction) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = isProduction ? "Production" : "Development";

        public string ApplicationName { get; set; } = "MARS.Videos365.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            null!;
    }
}
