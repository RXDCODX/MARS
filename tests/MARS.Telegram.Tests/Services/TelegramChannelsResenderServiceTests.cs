using System.Reflection;
using MARS.Telegram.Data;
using MARS.Telegram.Entities;
using MARS.Telegram.Services;
using MARS.Telegram.Services.PrivateChannelsResender;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TL;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Пересылка медиа из отслеживаемых каналов Telegram.
///
/// Сервис забирает пересланные сообщения из двух каналов, публикует их в своём
/// канале и удаляет оригиналы, чтобы те же мемы не копились снова. Проверяется, что
/// удаление происходит только после успешной отправки и что группа медиа уходит
/// альбомом, а не по одному файлу.
/// </summary>
public class TelegramChannelsResenderServiceTests
{
    private const long MonitoredChannelId = -1001803337348;
    private const long MonitoredPeerId = 1803337348;

    private readonly ChatTestDbContextFactory _factory = new();
    private readonly StartedLifetime _lifetime = new();
    private readonly FakeChannelClient _client = new();
    private readonly List<string> _errors = [];
    private readonly List<string> _warnings = [];

    /// <summary>
    /// При старте сервис создаёт состояние отслеживаемого канала и подписывается на
    /// обновления: без состояния пересылка пропустила бы уже накопленные сообщения,
    /// а без подписки — новые.
    /// </summary>
    [Fact]
    public async Task StartupCreatesChannelStateAndSubscribes()
    {
        var service = Create();

        await ExecuteAsync(service);
        await _lifetime.StartAsync();
        await WaitUntilAsync(() => _client.UpdateHandlers.Count > 0);

        await using var context = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        var state = await context.ChannelProcessingStates.FindAsync(
            [MonitoredChannelId],
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(state);
        Assert.Equal(0, state.OffsetId);
    }

    /// <summary>
    /// Уже накопленное пересланное сообщение публикуется и оригинал удаляется: иначе
    /// канал копил бы собственные старые сообщения и пересылал бы их повторно.
    /// </summary>
    [Fact]
    public async Task ForwardedPhotoIsResentAndDeleted()
    {
        await SaveStateAsync();
        var service = Create();
        _client.History = History(ForwardedPhotoMessage(messageId: 10));
        await InitializeAsync(service);
        Reset();

        await _client.RaiseUpdatesAsync(UpdateWith(ForwardedPhotoMessage(messageId: 10)));

        Assert.Single(_client.SentMedia);
        Assert.Equal(
            [
                [10],
            ],
            _client.Deleted.Select(ids => ids.ToArray()).ToArray()
        );
        Assert.Empty(_errors);
    }

    /// <summary>
    /// Сообщение без медиа игнорируется: пересылать нечего, а удалять оригинал
    /// нельзя — иначе текст пропал бы из канала.
    /// </summary>
    [Fact]
    public async Task ForwardedTextIsLeftInPlace()
    {
        await SaveStateAsync();
        var service = Create();
        _client.History = History(new TL.Message { id = 11, peer_id = ChannelPeer() });
        await InitializeAsync(service);

        await _client.RaiseUpdatesAsync(
            UpdateWith(
                new TL.Message
                {
                    id = 11,
                    peer_id = ChannelPeer(),
                    fwd_from = new MessageFwdHeader { from_name = "Источник" },
                    message = "просто текст",
                }
            )
        );

        Assert.Empty(_client.SentMedia);
        Assert.Empty(_client.Deleted);
    }

    /// <summary>
    /// Группа медиа уходит альбомом одним вызовом и удаляется одной пачкой: иначе
    /// зритель увидел бы десять отдельных картинок вместо одного поста.
    /// </summary>
    [Fact]
    public async Task GroupedMediaIsSentAsAlbum()
    {
        await SaveStateAsync();
        var service = Create();
        var first = GroupedPhotoMessage(messageId: 20, groupedId: 7);
        var second = GroupedPhotoMessage(messageId: 21, groupedId: 7);
        _client.History = History(first, second);
        await InitializeAsync(service);
        Reset();

        await _client.RaiseUpdatesAsync(UpdateWith(first));

        var album = Assert.Single(_client.Albums);
        Assert.Equal(2, album.Media.Count);
        Assert.Equal([20, 21], _client.Deleted.SelectMany(ids => ids).Order());
    }

    /// <summary>
    /// Сообщение из чужого канала игнорируется: сервис пересылает только из тех
    /// двух каналов, за которыми закреплён.
    /// </summary>
    [Fact]
    public async Task ForeignChannelMessageIsIgnored()
    {
        await SaveStateAsync();
        var service = Create();
        await InitializeAsync(service);

        await _client.RaiseUpdatesAsync(
            UpdateWith(
                new TL.Message
                {
                    id = 30,
                    peer_id = new PeerChannel { channel_id = 999 },
                    fwd_from = new MessageFwdHeader { from_name = "Чужой" },
                    media = new MessageMediaPhoto { photo = Photo() },
                }
            )
        );

        Assert.Empty(_client.SentMedia);
        Assert.Empty(_client.Deleted);
    }

    /// <summary>
    /// Сбой Telegram не роняет обработку: сообщение логируется, сервис продолжает
    /// ждать следующих обновлений.
    /// </summary>
    [Fact]
    public async Task FailureOfSendIsLogged()
    {
        await SaveStateAsync();
        var service = Create();
        _client.FailSend = true;
        await InitializeAsync(service);

        await _client.RaiseUpdatesAsync(UpdateWith(ForwardedPhotoMessage(messageId: 40)));

        Assert.NotEmpty(_errors);
    }

    /// <summary>
    /// Название канала берётся из списка чатов и кэшируется: без кэша каждый
    /// апдейт ходил бы в Telegram за одним и тем же названием.
    /// </summary>
    [Fact]
    public async Task ChannelTitleIsResolvedAndCached()
    {
        var service = Create();
        _client.ChannelTitle = "Канал мемов";
        await InitializeAsync(service);

        var first = Invoke<string>(service, "GetChannelTitle", [MonitoredPeerId]);
        var second = Invoke<string>(service, "GetChannelTitle", [MonitoredPeerId]);

        Assert.Equal("Канал мемов", first);
        Assert.Equal(first, second);
    }

    /// <summary>
    /// Канала нет в списке — используется идентификатор вместо пустого имени: в
    /// подписи альбома не должно быть пробелов вместо источника.
    /// </summary>
    [Fact]
    public async Task UnknownChannelFallsBackToItsId()
    {
        var service = Create();
        _client.ChannelTitle = null;
        await InitializeAsync(service);

        Assert.Equal(
            $"Channel:{MonitoredPeerId}",
            Invoke<string>(service, "GetChannelTitle", [MonitoredPeerId])
        );
    }

    /// <summary>
    /// Источник пересылки опознаётся по всем видам отправителя: без этого в
    /// подписи альбома оставалось бы «Unknown».
    /// </summary>
    [Theory]
    [InlineData("User:42", typeof(PeerUser))]
    [InlineData("Chat:7", typeof(PeerChat))]
    [InlineData("Name:Источник", null)]
    public async Task ForwardSourceIsDescribed(string expected, Type? peerType)
    {
        var service = Create();
        _client.ChannelTitle = "Канал мемов";
        await InitializeAsync(service);
        var header = new MessageFwdHeader
        {
            from_name = "Источник",
            from_id = peerType switch
            {
                null => null,
                _ when peerType == typeof(PeerUser) => new PeerUser { user_id = 42 },
                _ => new PeerChat { chat_id = 7 },
            },
        };

        var described = Invoke<string>(service, "GetForwardSourceInfo", [header]);

        Assert.Equal(expected, described);
    }

    /// <summary>
    /// Подпись альбома содержит текст оригинала и метаданные: без них в канале был
    /// бы набор картинок без единого слова.
    /// </summary>
    [Fact]
    public async Task GroupedCaptionKeepsTextAndMetadata()
    {
        var service = Create();
        _client.ChannelTitle = "Канал мемов";
        await InitializeAsync(service);
        var message = GroupedPhotoMessage(messageId: 50, groupedId: 8);
        message.message = "  подпись  ";
        // Источник назван каналом, а не пользователем: в подписи должно быть его
        // название из списка чатов, иначе в альбоме было бы «Channel:1803337348».
        message.fwd_from = new MessageFwdHeader { from_id = ChannelPeer() };

        var caption = Invoke<string>(service, "FormatGroupedCaption", [message]);

        Assert.StartsWith("подпись", caption);
        Assert.Contains("Канал мемов", caption);
        Assert.Contains("id: ", caption);
    }

    /// <summary>
    /// Пустой список удаляемых сообщений не вызывает Telegram: пустой вызов вернул
    /// бы ошибку и заспорил бы о том, что удалять.
    /// </summary>
    [Fact]
    public async Task NothingIsDeletedFromEmptyList()
    {
        var service = Create();
        await InitializeAsync(service);

        await InvokeAsync(
            service,
            "DeleteMessagesAsync",
            [new InputPeerChannel((long)MonitoredPeerId, 1), Array.Empty<int>()]
        );

        Assert.Empty(_client.Deleted);
    }

    /// <summary>
    /// Остановка сервиса отписывается от обновлений: иначе выключенный сервис
    /// продолжал бы пересылать медиа по подписке.
    /// </summary>
    [Fact]
    public async Task DisposeUnsubscribesFromUpdates()
    {
        var service = Create();
        await InitializeAsync(service);
        Assert.NotEmpty(_client.UpdateHandlers);

        service.Dispose();

        Assert.Empty(_client.UpdateHandlers);
    }

    /// <summary>
    /// Документ из группы уходит в альбом вместе с фото: иначе альбом получился бы
    /// неполным, а оригиналы всё равно удалились бы.
    /// </summary>
    [Fact]
    public async Task GroupedDocumentIsSentWithPhoto()
    {
        await SaveStateAsync();
        var service = Create();
        var photo = GroupedPhotoMessage(messageId: 60, groupedId: 9);
        var document = new TL.Message
        {
            id = 61,
            peer_id = ChannelPeer(),
            grouped_id = 9,
            fwd_from = new MessageFwdHeader { from_name = "Источник" },
            media = new MessageMediaDocument
            {
                document = new Document
                {
                    id = 500,
                    size = 8,
                    mime_type = "image/jpeg",
                    attributes = [new DocumentAttributeFilename { file_name = "мем.jpg" }],
                },
            },
        };
        _client.History = History(photo, document);
        await InitializeAsync(service);
        Reset();

        await _client.RaiseUpdatesAsync(UpdateWith(photo));

        var album = Assert.Single(_client.Albums);
        Assert.Equal(2, album.Media.Count);
        Assert.Equal([60, 61], _client.Deleted.SelectMany(ids => ids).Order());
    }

    /// <summary>
    /// Файл документа сохраняется под именем из атрибутов: иначе в канале появился
    /// бы файл с именем из id, и зритель не понял бы, что это.
    /// </summary>
    [Fact]
    public async Task DocumentIsUploadedUnderItsFileName()
    {
        await SaveStateAsync();
        var service = Create();
        var message = new TL.Message
        {
            id = 70,
            peer_id = ChannelPeer(),
            fwd_from = new MessageFwdHeader { from_name = "Источник" },
            message = "документ",
            media = new MessageMediaDocument
            {
                document = new Document
                {
                    id = 600,
                    size = 8,
                    mime_type = "image/jpeg",
                    attributes = [new DocumentAttributeFilename { file_name = "мем-документ.jpg" }],
                },
            },
        };
        _client.History = History(message);
        await InitializeAsync(service);

        await _client.RaiseUpdatesAsync(UpdateWith(message));

        Assert.NotEmpty(_client.UploadedFileNames);
        Assert.All(_client.UploadedFileNames, name => Assert.Contains("мем-документ", name));
    }

    /// <summary>
    /// Хеш истории зависит от идентификаторов сообщений: он отличает новую порцию
    /// сообщений от уже обработанной.
    /// </summary>
    private static T InvokeStatic<T>(string name, object?[] arguments)
    {
        var method = typeof(TelegramChannelsResenderService).GetMethod(
            name,
            BindingFlags.Static | BindingFlags.NonPublic
        )!;

        return (T)method.Invoke(null, arguments)!;
    }

    [Fact]
    public void MessagesHashDependsOnIds()
    {
        var first = InvokeStatic<long>(
            "CalculateMessagesHash",
            [
                new[]
                {
                    new TL.Message { id = 1 },
                    new TL.Message { id = 2 },
                },
            ]
        );
        var second = InvokeStatic<long>(
            "CalculateMessagesHash",
            [
                new[]
                {
                    new TL.Message { id = 1 },
                    new TL.Message { id = 3 },
                },
            ]
        );

        Assert.NotEqual(first, second);
    }

    /// <summary>
    /// Пустая история даёт нулевой хеш: иначе сервис счёл бы новой пустую порцию.
    /// </summary>
    [Fact]
    public void EmptyHistoryHasZeroHash()
    {
        Assert.Equal(0, InvokeStatic<long>("CalculateMessagesHash", [Array.Empty<TL.Message>()]));
    }

    private void Reset()
    {
        _client.SentMedia.Clear();
        _client.Albums.Clear();
        _client.Deleted.Clear();
        _client.UploadedFileNames.Clear();
    }

    private TelegramChannelsResenderService Create() =>
        new(
            new CollectingLogger(_errors, _warnings),
            new StubClientService(_client),
            _factory,
            _lifetime
        );

    private Task ExecuteAsync(TelegramChannelsResenderService service) =>
        InvokeAsync(service, "ExecuteAsync", [TestContext.Current.CancellationToken]);

    private Task InitializeAsync(TelegramChannelsResenderService service) =>
        InvokeAsync(service, "InitializeAsync", [TestContext.Current.CancellationToken]);

    private static TL.Message ForwardedPhotoMessage(int messageId) =>
        new()
        {
            id = messageId,
            peer_id = ChannelPeer(),
            fwd_from = new MessageFwdHeader { from_name = "Источник" },
            message = "мем",
            media = new MessageMediaPhoto { photo = Photo() },
        };

    private static TL.Message GroupedPhotoMessage(int messageId, long groupedId) =>
        new()
        {
            id = messageId,
            peer_id = ChannelPeer(),
            grouped_id = groupedId,
            fwd_from = new MessageFwdHeader { from_name = "Источник" },
            media = new MessageMediaPhoto { photo = Photo() },
        };

    private static PeerChannel ChannelPeer() => new() { channel_id = MonitoredPeerId };

    private static Photo Photo() =>
        new()
        {
            id = 77,
            sizes =
            [
                new PhotoSize
                {
                    type = "x",
                    w = 100,
                    h = 100,
                    size = 4,
                },
            ],
        };

    private static Messages_Messages History(params TL.Message[] messages) =>
        new() { messages = [.. messages] };

    private static Updates UpdateWith(TL.Message message) =>
        new() { updates = [new UpdateNewChannelMessage { message = message }] };

    private async Task SaveStateAsync()
    {
        await using var context = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        context.ChannelProcessingStates.Add(
            new ChannelProcessingState
            {
                ChannelId = MonitoredChannelId,
                OffsetId = 0,
                LastUpdated = DateTime.Now,
            }
        );
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100 && !condition(); attempt++)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }
    }

    private static Task InvokeAsync(
        TelegramChannelsResenderService service,
        string name,
        object?[] arguments
    ) =>
        (Task)
            typeof(TelegramChannelsResenderService)
                .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(service, arguments)!;

    private static T Invoke<T>(
        TelegramChannelsResenderService service,
        string name,
        object?[] arguments
    )
    {
        var method = typeof(TelegramChannelsResenderService).GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static
        )!;

        return (T)method.Invoke(method.IsStatic ? null : service, arguments)!;
    }

    /// <summary>
    /// Клиент-заглушка: настоящий <c>WTelegram.Client</c> авторизуется по номеру
    /// телефона и в тесте не собирается, поэтому проверяется сервис, а не сеть.
    /// </summary>
    private sealed class FakeChannelClient : IWTelegramChannelClient
    {
        public List<Func<UpdatesBase, Task>> UpdateHandlers { get; } = [];
        public List<string> SentMedia { get; } = [];
        public List<(ICollection<InputMedia> Media, string Caption)> Albums { get; } = [];
        public List<int[]> Deleted { get; } = [];
        public List<string> UploadedFileNames { get; } = [];
        public string? ChannelTitle { get; set; } = "Канал мемов";
        public Messages_Messages? History { get; set; }
        public bool FailSend { get; set; }

        public event Func<UpdatesBase, Task> OnUpdates
        {
            add => UpdateHandlers.Add(value);
            remove => UpdateHandlers.Remove(value);
        }

        public bool IsAuthorized => true;

        public Task RaiseUpdatesAsync(Updates updates) =>
            Task.WhenAll(UpdateHandlers.Select(handler => handler(updates)));

        public Task<Messages_Chats> Messages_GetAllChats() =>
            Task.FromResult(
                new Messages_Chats
                {
                    chats = new Dictionary<long, ChatBase>
                    {
                        [(long)MonitoredPeerId] = new Channel
                        {
                            id = MonitoredPeerId,
                            access_hash = 12345,
                            title = ChannelTitle,
                        },
                    },
                }
            );

        public Task<Messages_MessagesBase?> Messages_GetHistory(
            InputPeer peer,
            int offset_id,
            int add_offset,
            int limit
        ) => Task.FromResult<Messages_MessagesBase?>(History);

        public Task Messages_SendMedia(
            InputPeer peer,
            InputMedia media,
            string message,
            long random_id
        )
        {
            if (FailSend)
            {
                throw new InvalidOperationException("Telegram недоступен");
            }

            SentMedia.Add(message);
            return Task.CompletedTask;
        }

        public Task<bool> Messages_MarkDialogUnread(
            InputDialogPeerBase peer,
            bool unread = false
        ) => Task.FromResult(true);

        public Task Channels_DeleteMessages(InputChannelBase channel, int[] messageIds)
        {
            Deleted.Add(messageIds);
            return Task.CompletedTask;
        }

        public Task SendAlbumAsync(InputPeer peer, ICollection<InputMedia> album, string caption)
        {
            if (FailSend)
            {
                throw new InvalidOperationException("Telegram недоступен");
            }

            Albums.Add((album, caption));
            return Task.CompletedTask;
        }

        public Task<InputFileBase> UploadFileAsync(Stream stream, string fileName)
        {
            UploadedFileNames.Add(fileName);
            return Task.FromResult<InputFileBase>(new InputFile { id = 1 });
        }

        public Task<string> DownloadFileAsync(Document document, Stream output) =>
            Task.FromResult("image/jpeg");

        public Task<string> DownloadFileAsync(Photo photo, Stream output, PhotoSize size) =>
            Task.FromResult("jpeg");

        public void Dispose() { }
    }

    private sealed class StubClientService(IWTelegramChannelClient client) : IWTelegramClientService
    {
        public Task<WTelegramClientStatus> GetClientStatusAsync(
            CancellationToken cancellationToken = default
        ) => Task.FromResult(new WTelegramClientStatus { IsAuthenticated = true });

        public Task ReLoginAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public bool SubmitVerificationCode(string code) => false;

        public Task<IWTelegramChannelClient> GetClientAsync(
            CancellationToken cancellationToken = default
        ) => Task.FromResult(client);

        public Task HandleUpdate(
            global::Telegram.Bot.ITelegramBotClient botClient,
            global::Telegram.Bot.Types.Update? update
        ) => Task.CompletedTask;
    }

    private sealed class StartedLifetime : IHostApplicationLifetime
    {
        private readonly CancellationTokenSource _started = new();
        private readonly CancellationTokenSource _stopping = new();

        public CancellationToken ApplicationStarted => _started.Token;

        public CancellationToken ApplicationStopping => _stopping.Token;

        public CancellationToken ApplicationStopped => _stopping.Token;

        public Task StartAsync() => _started.CancelAsync();

        public void StopApplication() => _stopping.Cancel();
    }

    /// <summary>
    /// Логгер собирает ошибки и предупреждения: через них сервис сообщает о
    /// пропущенных сообщениях, иначе сбой был бы виден только по отсутствию
    /// пересланного медиа.
    /// </summary>
    private sealed class CollectingLogger(List<string> errors, List<string> warnings)
        : ILogger<TelegramChannelsResenderService>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            var message = formatter(state, exception);

            if (logLevel == LogLevel.Error)
            {
                errors.Add(message);
            }
            else if (logLevel == LogLevel.Warning)
            {
                warnings.Add(message);
            }
        }
    }

    [Fact]
    public async Task FakeClientExposesChannel()
    {
        _client.ChannelTitle = "Канал мемов";
        var chats = await _client.Messages_GetAllChats();
        var channel = chats.chats.Values.OfType<Channel>().FirstOrDefault();
        Assert.NotNull(channel);
        Assert.Equal("Канал мемов", channel.title);
        Assert.Equal(MonitoredPeerId, channel.id);
    }
}
