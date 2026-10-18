using System.Net;
using System.Text;
using MARS.Telegram.Configuration;
using MARS.Telegram.Models;
using MARS.Telegram.Services;
using MARS.Telegram.Services.GooglePhotos;
using MARS.Telegram.Services.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TL;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Загрузка фото из Telegram в Google Photos.
/// </summary>
/// <remarks>
/// <para>
/// Бот служит в отдельном чате, поэтому проверяется, что чужие чаты игнорируются и
/// что без авторизации загрузка не начинается: Google всё равно ответил бы отказом,
/// а пользователь увидел бы ошибку вместо объяснения.
/// </para>
/// <para>
/// Telegram подменяется заглушкой <see cref="ITelegramBotConnection"/>: методы
/// клиента WTelegram невиртуальные, и без обёртки проверка ушла бы в сеть.
/// </para>
/// </remarks>
public class TelegramGooglePhotosServiceTests
{
    private const long ChatId = -1001803337348;
    private const string SuccessJson =
        """{"results":[{"status":{"code":0},"mediaItem":{"id":"media-1"}}]}""";

    private readonly Mock<ITelegramBotConnection> _bot = new();

    [Fact]
    public async Task UnauthorizedChatGetsExplanation()
    {
        var service = Create(authorized: false);

        await service.HandleAsync(_bot.Object, PhotoMessage(), Token);

        Assert.Contains("не авторизован", SentText());
    }

    /// <summary>
    /// Чужой чат игнорируется: бот не отвечает в личных сообщениях пользователей.
    /// </summary>
    [Fact]
    public async Task ForeignChatIsIgnored()
    {
        var service = Create(authorized: true);

        await service.HandleAsync(_bot.Object, PhotoMessage(chatId: 42), Token);

        Assert.Empty(SentMessages());
    }

    [Fact]
    public async Task PhotoIsUploaded()
    {
        var service = Create(authorized: true, uploadOk: true);

        await service.HandleAsync(_bot.Object, PhotoMessage(), Token);

        Assert.Contains("успешно загружено", SentText());
    }

    /// <summary>
    /// Отказ Google показывается пользователю, а не проглатывается: иначе он
    /// повторил бы загрузку, не понимая причины.
    /// </summary>
    [Fact]
    public async Task UploadFailureIsReported()
    {
        var service = Create(authorized: true, uploadOk: false);

        await service.HandleAsync(_bot.Object, PhotoMessage(), Token);

        Assert.Contains("❌", SentText());
    }

    /// <summary>
    /// Сообщение без вложения игнорируется: загружать нечего.
    /// </summary>
    [Fact]
    public async Task MessageWithoutAttachmentIsIgnored()
    {
        var service = Create(authorized: true, uploadOk: true);

        var message = new TelegramIncomingMessage(
            ChatId,
            new InputPeerChat(Math.Abs(ChatId)),
            1,
            "просто текст",
            IsChannelPost: false,
            MediaGroupId: null,
            Photo: null,
            Document: null
        );

        await service.HandleAsync(_bot.Object, message, Token);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private IReadOnlyList<string> SentMessages() =>
        _bot
            .Invocations.Select(invocation =>
                invocation.Arguments.OfType<string>().FirstOrDefault()
            )
            .Where(text => text is not null)
            .Cast<string>()
            .ToArray();

    private string SentText() => string.Join(" | ", SentMessages());

    /// <summary>
    /// Фото в сообщении: у Telegram оно лежит в медиа, и обработчику нужен сам
    /// объект — по нему клиент и качает файл.
    /// </summary>
    private static TelegramIncomingMessage PhotoMessage(long chatId = ChatId) =>
        new(
            chatId,
            new InputPeerChat(Math.Abs(chatId)),
            1,
            string.Empty,
            IsChannelPost: false,
            MediaGroupId: null,
            new Photo
            {
                id = 1,
                sizes =
                [
                    new PhotoSize
                    {
                        type = "y",
                        w = 100,
                        h = 100,
                        size = 1024,
                    },
                ],
            },
            Document: null
        );

    private TelegramGooglePhotosService Create(bool authorized, bool uploadOk = true)
    {
        var auth = new Mock<IGooglePhotosAuthService>();
        auth.Setup(service => service.IsAuthorizedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorized);
        auth.Setup(service => service.GetValidAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorized ? "access-1" : null);

        StubConnection();

        var apiClient = new GooglePhotosApiClient(
            new SingleHandlerFactory(
                new StubHandler(
                    uploadOk ? HttpStatusCode.OK : HttpStatusCode.Forbidden,
                    SuccessJson
                )
            ),
            auth.Object,
            NullLogger<GooglePhotosApiClient>.Instance
        );

        return new TelegramGooglePhotosService(
            NullLogger<TelegramGooglePhotosService>.Instance,
            auth.Object,
            apiClient,
            new StubLifetime(),
            Options.Create(new GooglePhotosConfiguration { TelegramChatId = ChatId })
        );
    }

    /// <summary>
    /// Скачивание фото подменено: в сеть за него ходить нельзя, а проверяется
    /// реакция сервиса на результат.
    /// </summary>
    private void StubConnection() =>
        _bot.Setup(client =>
                client.DownloadFileAsync(
                    It.IsAny<Photo>(),
                    It.IsAny<Stream>(),
                    It.IsAny<PhotoSize>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(
                (Photo photo, Stream destination, PhotoSize size, CancellationToken _) =>
                {
                    destination.Write(Encoding.UTF8.GetBytes($"photo-{photo.id}-{size.size}"));

                    return Task.CompletedTask;
                }
            );

    private sealed class SingleHandlerFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests++;

            return Task.FromResult(
                new HttpResponseMessage(status) { Content = new StringContent(body) }
            );
        }
    }

    /// <summary>
    /// Журнал, который собирает ошибки: без него исключение внутри загрузки просто
    /// проглатывалось сервисом.
    /// </summary>
    private sealed class CollectingLogger<T> : ILogger<T>
    {
        public static List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => Messages.Add($"{logLevel}: {formatter(state, exception)} {exception}");
    }

    private sealed class StubLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;

        public CancellationToken ApplicationStopping => CancellationToken.None;

        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication() { }
    }
}
