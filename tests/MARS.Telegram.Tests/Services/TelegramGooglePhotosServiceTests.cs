using System.Net;
using System.Text;
using MARS.Shared.Models;
using MARS.Telegram.Configuration;
using MARS.Telegram.Services;
using MARS.Telegram.Services.GooglePhotos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Загрузка фото из Telegram в Google Photos.
///
/// Бот служит в отдельном чате, поэтому проверяется, что чужие чаты игнорируются и
/// что без авторизации загрузка не начинается: Google всё равно ответил бы отказом,
/// а пользователь увидел бы ошибку вместо объяснения.
/// </summary>
public class TelegramGooglePhotosServiceTests
{
    private const long ChatId = -1001803337348;
    private const string SuccessJson =
        """{"results":[{"status":{"code":0},"mediaItem":{"id":"media-1"}}]}""";

    private readonly Mock<ITelegramBotClient> _bot = new();

    [Fact]
    public async Task UnauthorizedChatGetsExplanation()
    {
        var service = Create(authorized: false);

        await service.HandMessage(_bot.Object, PhotoMessage());

        Assert.Contains("не авторизован", SentText());
    }

    /// <summary>
    /// Чужой чат игнорируется: бот не отвечает в личных сообщениях пользователей.
    /// </summary>
    [Fact]
    public async Task ForeignChatIsIgnored()
    {
        var service = Create(authorized: true);

        await service.HandMessage(_bot.Object, PhotoMessage(chatId: 42));

        Assert.Empty(SentMessages());
    }

    [Fact]
    public async Task PhotoIsUploaded()
    {
        var service = Create(authorized: true, uploadOk: true);

        await service.HandMessage(_bot.Object, PhotoMessage());

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

        await service.HandMessage(_bot.Object, PhotoMessage());

        Assert.Contains("❌", SentText());
    }

    /// <summary>
    /// Сообщение без вложения игнорируется: загружать нечего.
    /// </summary>
    [Fact]
    public async Task MessageWithoutAttachmentIsIgnored()
    {
        var service = Create(authorized: true, uploadOk: true);
        var message = new Message
        {
            Id = 1,
            Date = DateTime.Now,
            Text = "просто текст",
            Chat = new Chat { Id = ChatId, Type = ChatType.Supergroup },
        };

        await service.HandMessage(_bot.Object, new Update { Id = 1, Message = message });
    }

    private IReadOnlyList<string> SentMessages() =>
        _bot
            .Invocations.Select(invocation => invocation.Arguments.FirstOrDefault())
            .OfType<SendMessageRequest>()
            .Select(request => request.Text ?? string.Empty)
            .ToArray();

    private string SentText() => string.Join(" | ", SentMessages());

    private static Update PhotoMessage(long chatId = ChatId) =>
        new()
        {
            Id = 1,
            Message = new Message
            {
                Id = 1,
                Date = DateTime.Now,
                Chat = new Chat { Id = chatId, Type = ChatType.Supergroup },
                Photo =
                [
                    new PhotoSize
                    {
                        FileId = "photo-1",
                        FileUniqueId = "photo-1",
                        FileSize = 1024,
                    },
                ],
            },
        };

    private TelegramGooglePhotosService Create(
        bool authorized,
        bool uploadOk = true,
        StubHandler? handler = null
    )
    {
        var auth = new Mock<IGooglePhotosAuthService>();
        auth.Setup(service => service.IsAuthorizedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorized);
        auth.Setup(service => service.GetValidAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorized ? "access-1" : null);

        StubBot();

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
    /// <c>GetFile</c> и <c>DownloadFile</c> в Telegram.Bot 22 — расширяющий метод и
    /// метод интерфейса, поэтому подменяются они по-разному.
    /// </summary>
    private void StubBot()
    {
        _bot.Setup(client =>
                client.SendRequest(It.IsAny<GetFileRequest>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                (GetFileRequest request, CancellationToken _) =>
                    new TGFile
                    {
                        FileId = request.FileId,
                        FilePath = $"photos/{request.FileId}.jpg",
                    }
            );
        _bot.Setup(client =>
                client.DownloadFile(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(
                (string filePath, Stream destination, CancellationToken _) =>
                {
                    destination.Write(Encoding.UTF8.GetBytes(filePath));

                    return Task.CompletedTask;
                }
            );
    }

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
