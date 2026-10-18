using System.Reflection;
using System.Text;
using MARS.Telegram.Models;
using MARS.Telegram.Services;
using MARS.Telegram.Services.MemoryStorageService;
using MARS.Telegram.Services.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TL;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// «Буфер обмена»: по слову-триггеру в чате бот собирает картинки и отдаёт
/// ссылку на страницу с кнопкой «Скопировать все».
/// </summary>
/// <remarks>
/// <para>
/// Проверяется то, что пользователь и видит: приходит ли ссылка, ведёт ли она на
/// правильный адрес и выдаются ли файлы по id запроса.
/// </para>
/// <para>
/// Подключение — заглушка <see cref="ITelegramBotConnection"/>: методы
/// <c>WTelegram.Client</c> невиртуальные, и без обёртки проверка ушла бы в сеть.
/// Само скачивание тоже не проверяется — вместо него пишутся байты.
/// </para>
/// <para>
/// Буфер запросов приватный, поэтому записи добавляются рефлексией — иначе
/// неистёкший запрос нельзя получить без ожидания реального TTL.
/// </para>
/// </remarks>
[Collection("MemoryStorage")]
public class TelegramClipboardCopyServiceTests : IDisposable
{
    private const long ChatId = 100500;

    private readonly Mock<ITelegramBotConnection> _client = new(MockBehavior.Loose);
    private readonly TelegramClipboardCopyService _service;

    public TelegramClipboardCopyServiceTests()
    {
        MemoryStorage.ClearStorage();
        SetupFileTransfer(_client);
        _service = Create(new Dictionary<string, string?>());
    }

    public void Dispose() => MemoryStorage.ClearStorage();

    /// <summary>
    /// Заглушка файловой части: скачивание пишет в поток содержимое размера.
    /// </summary>
    private static void SetupFileTransfer(Mock<ITelegramBotConnection> client) =>
        client
            .Setup(instance =>
                instance.DownloadFileAsync(
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

    [Fact]
    public async Task TextWithoutTriggerIsIgnored()
    {
        await _service.HandleAsync(_client.Object, Incoming("просто текст"), Token);

        Assert.Empty(SentTexts());
    }

    [Theory]
    [InlineData("copy")]
    [InlineData("копи")]
    [InlineData("копипаста")]
    [InlineData("копировать")]
    [InlineData("Копировать вот это")]
    public async Task PhotoWithTriggerIsAccepted(string caption)
    {
        await _service.HandleAsync(_client.Object, Incoming(caption, photo: true), Token);

        Assert.Single(SentTexts());
    }

    /// <summary>
    /// Фото без триггера и без альбома не трогается: иначе бот складывал бы в
    /// буфер весь канал.
    /// </summary>
    [Fact]
    public async Task PhotoWithoutTriggerIsIgnored()
    {
        await _service.HandleAsync(_client.Object, Incoming("просто фото", photo: true), Token);

        Assert.Empty(SentTexts());
    }

    /// <summary>
    /// Успешный случай: ссылка на страницу приходит и файлы лежат в хранилище в
    /// памяти — именно их потом заберёт страница копирования.
    /// </summary>
    [Fact]
    public async Task PhotoWithTriggerProducesClipboardLink()
    {
        await _service.HandleAsync(_client.Object, Incoming("copy", photo: true), Token);

        var text = Assert.Single(SentTexts());
        Assert.Contains("Готово", text);
        Assert.Contains("telegram-copy", text);
        Assert.Equal(1, MemoryStorage.FileCount);
    }

    /// <summary>
    /// Жирный текст в ответе уходит сущностью, а не тегами: теги Telegram
    /// отобразил бы как текст.
    /// </summary>
    [Fact]
    public async Task Ответ_Содержит_Разметку_Сущностями()
    {
        await _service.HandleAsync(_client.Object, Incoming("copy", photo: true), Token);

        var entities = Entities();

        Assert.NotNull(entities);
        Assert.NotEmpty(entities);
        Assert.NotEmpty(entities);
        Assert.DoesNotContain("<b>", Assert.Single(SentTexts()));
    }

    /// <summary>
    /// Не скачав ни одного файла, бот честно сообщает об этом, а не присылает
    /// ссылку на пустую страницу.
    /// </summary>
    [Fact]
    public async Task FailedDownloadIsReported()
    {
        var client = new Mock<ITelegramBotConnection>(MockBehavior.Loose);
        client
            .Setup(instance =>
                instance.DownloadFileAsync(
                    It.IsAny<Photo>(),
                    It.IsAny<Stream>(),
                    It.IsAny<PhotoSize>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new InvalidOperationException("Telegram недоступен"));
        var service = Create(new Dictionary<string, string?>());

        await service.HandleAsync(client.Object, Incoming("copy", photo: true), Token);

        Assert.Contains("Не удалось скачать", Assert.Single(SentTexts(client)));
    }

    /// <summary>
    /// Недоступность Telegram при отправке не поднимается наружу: обработчик
    /// работает в фоне, и исключение уронило бы подписку целиком. Файл при этом
    /// остаётся в буфере — забрать его ещё можно.
    /// </summary>
    [Fact]
    public async Task SendFailureDoesNotThrow()
    {
        var client = new Mock<ITelegramBotConnection>(MockBehavior.Loose);
        client
            .Setup(instance =>
                instance.SendTextAsync(
                    It.IsAny<InputPeer>(),
                    It.IsAny<string>(),
                    It.IsAny<MessageEntity[]?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new InvalidOperationException("Telegram недоступен"));
        SetupFileTransfer(client);
        var service = Create(new Dictionary<string, string?>());

        await service.HandleAsync(client.Object, Incoming("copy", photo: true), Token);

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

        await service.HandleAsync(_client.Object, Incoming("copy", photo: true), Token);

        Assert.Contains("http://example.org", Assert.Single(SentTexts()));
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

        await service.HandleAsync(_client.Object, Incoming("copy", photo: true), Token);

        var text = Assert.Single(SentTexts());
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
        var first = _service.HandleAsync(
            _client.Object,
            Incoming("copy", photo: true, mediaGroupId: 77, messageId: 1),
            Token
        );
        var second = _service.HandleAsync(
            _client.Object,
            Incoming("copy", photo: true, mediaGroupId: 77, messageId: 2),
            Token
        );

        await Task.WhenAll(first, second);
        await Task.Delay(1500, Token);

        Assert.Single(SentTexts());
    }

    /// <summary>
    /// Сначала слово-триггер, потом альбом: сообщение без слов запускает ожидание
    /// на секунду, и фото, пришедшее в это время, обрабатывается. Триггер тоже
    /// запускается без `await` — иначе он истёк бы до прихода фото.
    /// </summary>
    [Fact]
    public async Task TriggerThenAlbumIsAccepted()
    {
        var trigger = _service.HandleAsync(_client.Object, Incoming("копи"), Token);

        await _service.HandleAsync(
            _client.Object,
            Incoming(null, photo: true, messageId: 5),
            Token
        );
        await trigger;

        Assert.Single(SentTexts());
    }

    /// <summary>
    /// Триггер, на который не пришло фото, истекает сам: иначе следующее фото
    /// считалось бы «продолжением» старого альбома.
    /// </summary>
    [Fact]
    public async Task TriggerExpiresWithoutMedia()
    {
        await _service.HandleAsync(_client.Object, Incoming("копи"), Token);

        await Task.Delay(1500, Token);

        await _service.HandleAsync(
            _client.Object,
            Incoming(null, photo: true, messageId: 9),
            Token
        );

        Assert.Empty(SentTexts());
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

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static TelegramClipboardCopyService Create(Dictionary<string, string?> settings) =>
        new(
            NullLogger<TelegramClipboardCopyService>.Instance,
            new StubLifetime(),
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build()
        );

    /// <summary>
    /// Отправленные тексты собираются из вызовов подключения: разметка уходит
    /// сущностями, поэтому в тексте её быть не должно.
    /// </summary>
    private string[] SentTexts() => SentTexts(_client);

    private static string[] SentTexts(Mock<ITelegramBotConnection> client) =>
        client
            .Invocations.Select(invocation =>
                invocation.Arguments.OfType<string>().FirstOrDefault()
            )
            .Where(text => text is not null)
            .Cast<string>()
            .ToArray();

    /// <summary>
    /// Сущности из отправленных сообщений. Отбор с проверкой на отсутствие: первым
    /// идёт вызов скачивания, а у него сущностей нет вовсе, и без фильтра внешний
    /// <c>FirstOrDefault</c> вернул бы именно это отсутствие.
    /// </summary>
    private MessageEntity[]? Entities() =>
        _client
            .Invocations.Select(invocation =>
                invocation.Arguments.OfType<MessageEntity[]>().FirstOrDefault()
            )
            .FirstOrDefault(entities => entities is not null);

    /// <summary>
    /// Входящее сообщение в том виде, в котором его отдаёт разбор обновлений.
    /// </summary>
    private static TelegramIncomingMessage Incoming(
        string? text,
        bool photo = false,
        long? mediaGroupId = null,
        int messageId = 1
    )
    {
        TL.Photo? picture = null;

        if (photo)
        {
            picture = new Photo
            {
                id = messageId,
                sizes =
                [
                    new PhotoSize
                    {
                        type = "y",
                        w = 90,
                        h = 90,
                        size = 1000 + messageId,
                    },
                ],
            };
        }

        return new TelegramIncomingMessage(
            ChatId,
            new InputPeerUser(ChatId, 0),
            messageId,
            text ?? string.Empty,
            IsChannelPost: false,
            mediaGroupId,
            picture,
            Document: null
        );
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
