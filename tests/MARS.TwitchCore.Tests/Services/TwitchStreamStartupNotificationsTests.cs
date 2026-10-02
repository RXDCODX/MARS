using System.Net;
using MARS.TwitchCore.Configuration;
using MARS.TwitchCore.Services.StreamBotNotifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Уведомления в чат при старте стрима.
///
/// Проверяется главное: если аудиоконтроллер не поднялся, зрители получают
/// напоминание. Без него звуковые запросы просто не работали бы, и никто бы
/// не понял почему.
/// </summary>
public class TwitchStreamStartupNotificationsTests
{
    [Fact]
    public async Task MissingAudioControllerIsReportedToChat()
    {
        var client = ConnectedClient();
        var notifications = Create(client.Object, http: Unreachable());

        await InvokeAsync(notifications, "PubSubOnlineOnStreamUp");

        client.Verify(
            instance =>
                instance.SendMessageAsync(It.IsAny<JoinedChannel>(), It.IsAny<string>(), default),
            Times.AtLeastOnce
        );
    }

    /// <summary>
    /// Здоровый аудиоконтроллер не порождает сообщений: при каждом старте стрима
    /// в чат улетало бы напоминание обратном.
    /// </summary>
    [Fact]
    public async Task HealthyAudioControllerStaysQuiet()
    {
        var client = ConnectedClient();
        var notifications = Create(client.Object, http: Healthy());

        await InvokeAsync(notifications, "PubSubOnlineOnStreamUp");

        client.Verify(
            instance =>
                instance.SendMessageAsync(It.IsAny<JoinedChannel>(), It.IsAny<string>(), default),
            Times.Never
        );
    }

    [Fact]
    public async Task OfflineStreamIsAnnounced()
    {
        var client = ConnectedClient();
        var notifications = Create(client.Object, http: Healthy());

        await InvokeAsync(notifications, "PubSibOfflineStream");

        client.Verify(
            instance =>
                instance.SendMessageAsync(It.IsAny<JoinedChannel>(), It.IsAny<string>(), default),
            Times.Once
        );
    }

    /// <summary>
    /// Health-check ходит на порт из конфигурации окружения: в проде и в
    /// разработке это разные порты, и проверка неверного молча даёт «не
    /// запущен» на рабочей машине.
    /// </summary>
    [Fact]
    public async Task HealthCheckUsesConfiguredPort()
    {
        var factory = new Mock<IHttpClientFactory>();
        var handler = new RecordingHandler(HttpStatusCode.OK);
        factory
            .Setup(instance => instance.CreateClient("audio-controller-health"))
            .Returns(() => new HttpClient(handler, disposeHandler: false));
        var notifications = Create(
            Mock.Of<ITwitchClient>(),
            http: factory.Object,
            options: new AudioControllerOptions { AudioControllerDevPort = 39999 }
        );

        await InvokeAsync(notifications, "PubSubOnlineOnStreamUp");

        Assert.Contains("39999", handler.LastUrl);
    }

    [Fact]
    public async Task StartAndStopAreNoOps()
    {
        var notifications = Create(Mock.Of<ITwitchClient>(), http: Healthy());

        await notifications.StartAsync(TestContext.Current.CancellationToken);
        await notifications.StopAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Обработчики событий внутренние и подписаны на EventSub, поэтому вызываются
    /// напрямую — так же, как их зовёт Twitch.
    /// </summary>
    private static Task InvokeAsync(TwitchStreamStartupNotifications notifications, string handler)
    {
        var method = typeof(TwitchStreamStartupNotifications).GetMethod(
            handler,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        )!;

        return (Task)method.Invoke(notifications, [null, null])!;
    }

    /// <summary>
    /// Канал должен быть «подключён»: расширение отправки читает список
    /// подключённых каналов и сам ищет канал через GetJoinedChannel, а без обоих
    /// ответов оно молча уходит, не отправив ничего.
    /// </summary>
    private static Mock<ITwitchClient> ConnectedClient()
    {
        var client = new Mock<ITwitchClient>();
        client
            .SetupGet(instance => instance.JoinedChannels)
            .Returns([new JoinedChannel(MARS.TwitchCore.Extensions.TwitchConstants.Channel)]);
        client
            .Setup(instance => instance.GetJoinedChannel(It.IsAny<string>()))
            .Returns(new JoinedChannel(MARS.TwitchCore.Extensions.TwitchConstants.Channel));
        client
            .Setup(instance =>
                instance.SendMessageAsync(It.IsAny<JoinedChannel>(), It.IsAny<string>(), default)
            )
            .Returns(Task.CompletedTask);

        return client;
    }

    private static TwitchStreamStartupNotifications Create(
        ITwitchClient client,
        IHttpClientFactory http,
        AudioControllerOptions? options = null
    ) =>
        new(
            NullLogger<TwitchStreamStartupNotifications>.Instance,
            client,
            new TestLifetime(),
            OfflineEventSub.Create(),
            Microsoft.Extensions.Options.Options.Create(options ?? new AudioControllerOptions()),
            new StubEnvironment(),
            http
        );

    private static IHttpClientFactory Unreachable()
    {
        var factory = new Mock<IHttpClientFactory>();
        factory
            .Setup(instance => instance.CreateClient("audio-controller-health"))
            .Returns(() => new HttpClient(new ThrowingHandler()));

        return factory.Object;
    }

    private static IHttpClientFactory Healthy()
    {
        var factory = new Mock<IHttpClientFactory>();
        factory
            .Setup(instance => instance.CreateClient("audio-controller-health"))
            .Returns(() =>
                new HttpClient(new RecordingHandler(HttpStatusCode.OK), disposeHandler: false)
            );

        return factory.Object;
    }

    private sealed class RecordingHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public string LastUrl { get; private set; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            LastUrl = request.RequestUri?.ToString() ?? string.Empty;

            return Task.FromResult(new HttpResponseMessage(status));
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => throw new HttpRequestException("аудиоконтроллер не запущен");
    }

    private sealed class StubEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "MARS.TwitchCore.Tests";

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public string EnvironmentName { get; set; } = Environments.Development;
    }
}
