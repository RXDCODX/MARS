using System.Reflection;
using System.Text;
using MARS.Telegram.Services;
using MARS.Telegram.Services.MemoryStorageService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// «Буфер обмена»: по слову-триггеру в чате бот собирает картинки и отдаёт
/// ссылку на страницу с кнопкой «Скопировать все».
///
/// Проверяется то, что пользователь и видит: приходит ли ссылка, ведёт ли она на
/// правильный адрес и выдаются ли файлы по id запроса.
///
/// Заглушка перехватывает <c>SendRequest</c>, а не <c>SendMessage</c>: в
/// Telegram.Bot <c>SendMessage</c> и <c>GetFile</c> — расширяющие методы поверх
/// интерфейса, а <c>DownloadFile</c> — метод интерфейса и заглушается напрямую.
/// Само скачивание не проверяется: оно ушло бы в сеть.
///
/// Буфер запросов приватный, поэтому записи добавляются рефлексией — иначе
/// неистёкший запрос нельзя получить без ожидания реального TTL.
/// </summary>
/// <summary>
/// Хранилище в памяти статическое и общее для всего процесса, поэтому все
/// тесты, работающие с ним, обязаны идти в одной коллекции: иначе они идут
/// параллельно и видят файлы друг друга (тест очищает хранилище — и чужой тест
/// падает на отсутствии своего файла).
/// </summary>
[Collection("MemoryStorage")]
public class TelegramClipboardCopyServiceTests : IDisposable
{
    private const long ChatId = 100500;

    private readonly Mock<ITelegramBotClient> _client = new(MockBehavior.Loose);
    private readonly TelegramClipboardCopyService _service;

    public TelegramClipboardCopyServiceTests()
    {
        MemoryStorage.ClearStorage();
        SetupFileTransfer(_client);
        _service = Create(new Dictionary<string, string?>());
    }

    public void Dispose() => MemoryStorage.ClearStorage();

    /// <summary>
    /// Заглушка файловой части Telegram: путь файла и его содержимое.
    /// </summary>
    private static void SetupFileTransfer(Mock<ITelegramBotClient> client)
    {
        client
            .Setup(instance =>
                instance.SendRequest(It.IsAny<GetFileRequest>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                (GetFileRequest request, CancellationToken _) =>
                    new TGFile
                    {
                        FileId = request.FileId,
                        FilePath = $"photos/{request.FileId}.jpg",
                    }
            );
        client
            .Setup(instance =>
                instance.DownloadFile(
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

    /// <summary>
    /// Событие не о сообщении игнорируется: в бот приходят ещё и нажатия кнопок,
    /// и апдейты чатов.
    /// </summary>
    [Fact]
    public async Task NonMessageUpdateIsIgnored()
    {
        await _service.HandMessage(
            _client.Object,
            new Update { CallbackQuery = new CallbackQuery { Id = "1" } }
        );

        Assert.Empty(SentMessages());
    }

    [Fact]
    public async Task TextWithoutTriggerIsIgnored()
    {
        await _service.HandMessage(_client.Object, Update("просто текст"));

        Assert.Empty(SentMessages());
    }

    [Theory]
    [InlineData("copy")]
    [InlineData("копи")]
    [InlineData("копипаста")]
    [InlineData("копировать")]
    [InlineData("Копировать вот это")]
    public async Task PhotoWithTriggerIsAccepted(string caption)
    {
        await _service.HandMessage(_client.Object, Update(caption, photo: true));

        Assert.Single(SentMessages());
    }

    /// <summary>
    /// Фото без триггера и без альбома не трогается: иначе бот складывал бы в
    /// буфер весь канал.
    /// </summary>
    [Fact]
    public async Task PhotoWithoutTriggerIsIgnored()
    {
        await _service.HandMessage(_client.Object, Update("просто фото", photo: true));

        Assert.Empty(SentMessages());
    }

    /// <summary>
    /// Успешный случай: ссылка на страницу приходит и файлы лежат в хранилище в
    /// памяти — именно их потом заберёт страница копирования.
    /// </summary>
    [Fact]
    public async Task PhotoWithTriggerProducesClipboardLink()
    {
        await _service.HandMessage(_client.Object, Update("copy", photo: true));

        var message = Assert.Single(SentMessages());
        Assert.Contains("Готово", message.Text);
        Assert.Contains("telegram-copy", message.Text);
        Assert.Equal(1, MemoryStorage.FileCount);
    }

    /// <summary>
    /// Не скачав ни одного файла, бот честно сообщает об этом, а не присылает
    /// ссылку на пустую страницу.
    /// </summary>
    [Fact]
    public async Task FailedDownloadIsReported()
    {
        var client = new Mock<ITelegramBotClient>(MockBehavior.Loose);
        var service = Create(new Dictionary<string, string?>());

        await service.HandMessage(client.Object, Update("copy", photo: true));

        Assert.Contains("Не удалось скачать", Assert.Single(SentMessages(client)).Text);
    }

    /// <summary>
    /// Недоступность Telegram при отправке не поднимается наружу: обработчик
    /// работает в фоне, и исключение уронило бы подписку целиком. Файл при этом
    /// остаётся в буфере — забрать его ещё можно.
    /// </summary>
    [Fact]
    public async Task SendFailureDoesNotThrow()
    {
        var client = new Mock<ITelegramBotClient>(MockBehavior.Loose);
        client
            .Setup(instance =>
                instance.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new InvalidOperationException("Telegram недоступен"));
        SetupFileTransfer(client);
        var service = Create(new Dictionary<string, string?>());

        await service.HandMessage(client.Object, Update("copy", photo: true));

        Assert.Equal(1, MemoryStorage.FileCount);
    }

    /// <summary>
    /// Адрес страницы берётся из конфигурации, а не собирается вслепую: за
    /// контейнером это внешний адрес, и ссылка вида <c>http://+:8080</c> зрителю
    /// не открылась бы.
    /// </summary>
    [Fact]
    public async Task ClipboardUrlUsesConfiguredBaseUrl()
    {
        var service = Create(
            new Dictionary<string, string?>
            {
                ["AppSettings:PublicBaseUrl"] = "http://example.org/",
            }
        );

        await service.HandMessage(_client.Object, Update("copy", photo: true));

        Assert.Contains("http://example.org", Assert.Single(SentMessages()).Text);
    }

    /// <summary>
    /// Служебные адреса вида <c>+</c> и <c>0.0.0.0</c> заменяются на localhost:
    /// из браузера зрителя они не открываются.
    /// </summary>
    [Theory]
    [InlineData("http://+:8080/")]
    [InlineData("http://0.0.0.0:8080/")]
    [InlineData("http://*:8080/")]
    public async Task ServiceAddressesAreRewritten(string configured)
    {
        var service = Create(new Dictionary<string, string?> { ["PublicBaseUrl"] = configured });

        await service.HandMessage(_client.Object, Update("copy", photo: true));

        var text = Assert.Single(SentMessages()).Text;
        Assert.Contains("localhost", text);
        Assert.DoesNotContain("0.0.0.0", text);
        Assert.DoesNotContain("+:", text);
    }

    [Fact]
    public async Task UnknownRequestIdYieldsError()
    {
        var result = await _service.GetFileUrlsByRequestIdAsync("нет-такого");

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankRequestIdYieldsError(string? requestId)
    {
        var urls = await _service.GetFileUrlsByRequestIdAsync(requestId!);
        var completed = await _service.MarkRequestAsCompletedAsync(requestId!);

        Assert.False(urls.Success);
        Assert.False(completed.Success);
    }

    /// <summary>
    /// Файлы запроса отдаются по виртуальным путям хранилища в памяти: страница
    /// копирования забирает их именно оттуда.
    /// </summary>
    [Fact]
    public async Task StoredRequestReturnsMemoryUrls()
    {
        var fileName = "telegram-copy/запрос/01.png";
        await MemoryStorage.AddFileAsync(fileName, [1, 2, 3]);
        AddRequest(_service, "запрос", fileName);

        var result = await _service.GetFileUrlsByRequestIdAsync("запрос");

        Assert.True(result.Success);
        Assert.Equal("/memory/" + Uri.EscapeDataString(fileName), result.Result![0]);
    }

    [Fact]
    public async Task ExpiredRequestIsDropped()
    {
        var fileName = "telegram-copy/старый/01.png";
        await MemoryStorage.AddFileAsync(fileName, [1, 2, 3]);
        AddRequest(_service, "старый", fileName, DateTimeOffset.Now.AddHours(-2));

        var result = await _service.GetFileUrlsByRequestIdAsync("старый");

        Assert.False(result.Success);
        Assert.False(MemoryStorage.FileExists(fileName));
    }

    [Fact]
    public async Task CompletedRequestIsRemovedWithItsFiles()
    {
        var fileName = "telegram-copy/готовый/01.png";
        await MemoryStorage.AddFileAsync(fileName, [1, 2, 3]);
        AddRequest(_service, "готовый", fileName);

        var result = await _service.MarkRequestAsCompletedAsync("готовый");

        Assert.True(result.Success);
        Assert.False(MemoryStorage.FileExists(fileName));
    }

    [Fact]
    public async Task CompletingUnknownRequestFails()
    {
        var result = await _service.MarkRequestAsCompletedAsync("нет-такого");

        Assert.False(result.Success);
    }

    /// <summary>
    /// Альбом из нескольких фото обрабатывается один раз: без дебаунса в буфер
    /// попадали бы дубли одного альбома, и пользователь получил бы несколько
    /// ссылок вместо одной.
    ///
    /// Сообщения запускаются одновременно, а не по очереди: дебаунс работает
    /// только пока предыдущее сообщение ещё ждёт, и при последовательном
    /// `await` каждый файл обрабатывался бы отдельной ссылкой.
    /// </summary>
    [Fact]
    public async Task MediaGroupIsProcessedOnce()
    {
        var first = _service.HandMessage(
            _client.Object,
            Update("copy", photo: true, mediaGroupId: "группа-1", messageId: 1)
        );
        var second = _service.HandMessage(
            _client.Object,
            Update("copy", photo: true, mediaGroupId: "группа-1", messageId: 2)
        );

        await Task.WhenAll(first, second);
        await Task.Delay(1500, TestContext.Current.CancellationToken);

        Assert.Single(SentMessages());
    }

    /// <summary>
    /// Сначала слово-триггер, потом альбом: сообщение без слов запускает ожидание
    /// на секунду, и фото, пришедшее в это время, обрабатывается. Триггер тоже
    /// запускается без `await` — иначе он истёк бы до прихода фото.
    /// </summary>
    [Fact]
    public async Task TriggerThenAlbumIsAccepted()
    {
        var trigger = _service.HandMessage(_client.Object, Update("копи", photo: false));

        await _service.HandMessage(_client.Object, Update(null, photo: true, messageId: 5));
        await trigger;

        Assert.Single(SentMessages());
    }

    /// <summary>
    /// Триггер, на который не пришло фото, истекает сам: иначе следующее фото
    /// считалось бы «продолжением» старого альбома.
    /// </summary>
    [Fact]
    public async Task TriggerExpiresWithoutMedia()
    {
        await _service.HandMessage(_client.Object, Update("копи", photo: false));

        await Task.Delay(1500, TestContext.Current.CancellationToken);

        await _service.HandMessage(_client.Object, Update(null, photo: true, messageId: 9));

        Assert.Empty(SentMessages());
    }

    /// <summary>
    /// TTL из конфигурации влияет на срок жизни запроса: без него ссылка жила бы
    /// всегда и держала файлы в памяти сервиса.
    /// </summary>
    [Fact]
    public async Task ShortTtlDropsRequest()
    {
        var service = Create(
            new Dictionary<string, string?>
            {
                ["AppSettings:TelegramClipboardCopy:RequestTtlMinutes"] = "1",
            }
        );
        var fileName = "telegram-copy/быстро/01.png";
        await MemoryStorage.AddFileAsync(fileName, [1]);
        AddRequest(service, "быстро", fileName, DateTimeOffset.Now.AddMinutes(-2));

        var result = await service.GetFileUrlsByRequestIdAsync("быстро");

        Assert.False(result.Success);
    }

    /// <summary>
    /// Мусор в значении TTL не ломает сервис: используется зашивка в полчаса.
    /// </summary>
    [Fact]
    public async Task UnusableTtlFallsBackToDefault()
    {
        var service = Create(
            new Dictionary<string, string?>
            {
                ["AppSettings:TelegramClipboardCopy:RequestTtlMinutes"] = "не число",
            }
        );
        var fileName = "telegram-copy/свежий/01.png";
        await MemoryStorage.AddFileAsync(fileName, [1]);
        AddRequest(service, "свежий", fileName, DateTimeOffset.Now.AddMinutes(-25));

        var result = await service.GetFileUrlsByRequestIdAsync("свежий");

        Assert.True(result.Success);
    }

    private static TelegramClipboardCopyService Create(Dictionary<string, string?> settings) =>
        new(
            NullLogger<TelegramClipboardCopyService>.Instance,
            new StubLifetime(),
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build()
        );

    /// <summary>
    /// Отправленные сообщения собираются из вызовов <c>SendRequest</c>: так же,
    /// как в <c>TelegramLoggerTests</c> в MARS.Admin.Tests.
    /// </summary>
    private SendMessageRequest[] SentMessages() => SentMessages(_client);

    private static SendMessageRequest[] SentMessages(Mock<ITelegramBotClient> client) =>
        client
            .Invocations.Select(invocation => invocation.Arguments.FirstOrDefault())
            .OfType<SendMessageRequest>()
            .ToArray();

    private static Update Update(
        string? text,
        bool photo = false,
        string? mediaGroupId = null,
        int messageId = 1
    )
    {
        var message = new Message
        {
            Id = messageId,
            Chat = new Chat { Id = ChatId },
            Text = text,
            Caption = text,
            MediaGroupId = mediaGroupId,
        };

        if (photo)
        {
            message.Photo =
            [
                new PhotoSize { FileId = $"file-{messageId}", FileUniqueId = $"file-{messageId}" },
            ];
        }

        return new Update { Message = message };
    }

    /// <summary>
    /// Запрос кладётся в приватный словарь так же, как его кладёт обработчик фото.
    /// </summary>
    private static void AddRequest(
        TelegramClipboardCopyService service,
        string requestId,
        string memoryFileName,
        DateTimeOffset? createdAt = null
    )
    {
        var requests = typeof(TelegramClipboardCopyService)
            .GetField("_clipboardRequests", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(service)!;
        var filesType = requests.GetType().GetGenericArguments()[1];

        requests
            .GetType()
            .GetMethod("TryAdd")!
            .Invoke(
                requests,
                [
                    requestId,
                    Activator.CreateInstance(
                        filesType,
                        [new[] { memoryFileName }, createdAt ?? DateTimeOffset.Now]
                    ),
                ]
            );
    }

    private sealed class StubLifetime : IHostApplicationLifetime
    {
        private readonly CancellationTokenSource _stopping = new();

        public CancellationToken ApplicationStarted => CancellationToken.None;

        public CancellationToken ApplicationStopping => _stopping.Token;

        public CancellationToken ApplicationStopped => _stopping.Token;

        public void StopApplication() => _stopping.Cancel();
    }
}
