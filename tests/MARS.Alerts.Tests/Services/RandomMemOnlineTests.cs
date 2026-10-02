using System.Reflection;
using MARS.Alerts.Configuration;
using MARS.Alerts.Services.PyroAlerts;
using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TL;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Сбор мемов из отслеживаемых каналов Telegram.
///
/// Мемы приходят как фото, картинки и видео; в очередь попадают только разрешённые
/// каналы и только сообщения без ссылок — иначе в очередь уехала бы реклама.
/// Проверяется, что в очередь уходит ровно то, что ожидается, с текстом автора.
/// </summary>
public class RandomMemOnlineTests
{
    private const long AllowedChannelId = 42;

    private readonly FakeClient _client = new();
    private readonly List<MediaDto> _queued = [];
    private readonly Mock<ITelegramusNotifier> _notifier = new(MockBehavior.Loose);
    private readonly StartedLifetime _lifetime = new();

    /// <summary>
    /// Фото из разрешённого канала попадает в очередь с типом «изображение» и
    /// подписью автора: очередь озвучивает текст вслух.
    /// </summary>
    [Fact]
    public async Task PhotoFromAllowedChannelIsQueued()
    {
        var service = Create();
        await StartAsync(service);

        await HandleAsync(
            service,
            new UpdateNewChannelMessage
            {
                message = new TL.Message
                {
                    id = 1,
                    message = "смешной мем",
                    entities = [],
                    peer_id = new PeerChat { chat_id = AllowedChannelId },
                    media = new MessageMediaPhoto { photo = Photo() },
                },
            }
        );

        var dto = Assert.Single(_queued);
        Assert.Equal(MediaType.Image, dto.MediaInfo.FileInfo.Type);
        Assert.Equal("смешной мем", dto.MediaInfo.TextInfo.Text);
        Assert.Contains("77", dto.MediaInfo.FileInfo.FileName);
    }

    /// <summary>
    /// Сообщение из другого канала игнорируется: в очередь попадают только мемы из
    /// разрешённых каналов.
    /// </summary>
    [Fact]
    public async Task MessageFromForeignChannelIsIgnored()
    {
        var service = Create();
        await StartAsync(service);

        await HandleAsync(
            service,
            UpdateWithMessage(id: 2, peerId: 999, media: new MessageMediaPhoto { photo = Photo() })
        );

        Assert.Empty(_queued);
    }

    /// <summary>
    /// Сообщение со ссылкой игнорируется: так в очередь не попадают рекламные
    /// картинки из комментариев.
    /// </summary>
    [Fact]
    public async Task MessageWithLinkIsIgnored()
    {
        var service = Create();
        await StartAsync(service);

        var message = UpdateWithMessage(
            id: 3,
            peerId: AllowedChannelId,
            new MessageMediaPhoto { photo = Photo() }
        );
        ((TL.Message)message.message!).entities = [new MessageEntityUrl()];

        await HandleAsync(service, message);

        Assert.Empty(_queued);
    }

    /// <summary>
    /// Видео кладётся в очередь как видео: иначе в OBS ушёл бы файл без звука,
    /// помеченный как картинка.
    /// </summary>
    [Fact]
    public async Task VideoDocumentIsQueuedAsVideo()
    {
        var service = Create();
        await StartAsync(service);

        await HandleAsync(
            service,
            UpdateWithMessage(
                id: 4,
                peerId: AllowedChannelId,
                new MessageMediaDocument
                {
                    document = new Document
                    {
                        id = 88,
                        size = 16,
                        mime_type = "video/mp4",
                    },
                }
            )
        );

        var queued = Assert.Single(_notifier.Invocations, call => call.Method.Name == "RandomMem");
        var dto = queued.Arguments.OfType<MediaDto>().Single();
        Assert.Equal(MediaType.Video, dto.MediaInfo.FileInfo.Type);
    }

    /// <summary>
    /// Документ неизвестного типа игнорируется: в очередь попадают только те
    /// форматы, которые умеет показывать оверлей.
    /// </summary>
    [Fact]
    public async Task UnknownDocumentTypeIsIgnored()
    {
        var service = Create();
        await StartAsync(service);

        await HandleAsync(
            service,
            UpdateWithMessage(
                id: 5,
                peerId: AllowedChannelId,
                new MessageMediaDocument
                {
                    document = new Document
                    {
                        id = 89,
                        size = 4,
                        mime_type = "application/zip",
                        attributes = [],
                    },
                }
            )
        );

        Assert.Empty(_queued);
    }

    /// <summary>
    /// Остановленный сервис не реагирует на обновления: иначе выключатель
    /// «собирать мемы» не работал бы.
    /// </summary>
    [Fact]
    public async Task StoppedServiceIgnoresUpdates()
    {
        var service = Create();
        await StartAsync(service);
        service.IsStop = true;

        await _client.RaiseAsync(
            new Updates
            {
                updates =
                [
                    UpdateWithMessage(
                        id: 6,
                        peerId: AllowedChannelId,
                        new MessageMediaPhoto { photo = Photo() }
                    ),
                ],
            }
        );

        Assert.Empty(_queued);
    }

    /// <summary>
    /// Остановка сервиса освобождает клиента: иначе фоновые обработчики WTelegram
    /// держали бы сокет открытым до конца контейнера.
    /// </summary>
    [Fact]
    public async Task DisposeReleasesClient()
    {
        var service = Create();
        await StartAsync(service);

        service.Dispose();

        Assert.True(_client.Disposed);
    }

    private RandomMemOnline Create()
    {
        _notifier
            .Setup(notifier => notifier.RandomMem(It.IsAny<MediaDto>()))
            .Callback<MediaDto>(dto => _queued.Add(dto))
            .Returns(Task.CompletedTask);

        return new RandomMemOnline(
            _lifetime,
            Options.Create(
                new WTelegramConfiguration
                {
                    ApiHash = "hash",
                    PhoneNumber = "+10000000000",
                    Password = "pass",
                    AllowedChannelIds = [AllowedChannelId],
                }
            ),
            new StubFactory(_client),
            _notifier.Object,
            NullLogger<RandomMemOnline>.Instance
        );
    }

    /// <summary>
    /// Подписка на обновления происходит по старту приложения: до него клиент
    /// создавать рано, а после — пропущенные сообщения уже не вернуть.
    /// </summary>
    private async Task StartAsync(RandomMemOnline service)
    {
        await InvokeAsync(service, "ExecuteAsync", [TestContext.Current.CancellationToken]);
        await _lifetime.StartAsync();

        for (var attempt = 0; attempt < 100 && _client.Handlers.Count == 0; attempt++)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }
    }

    private static UpdateNewChannelMessage UpdateWithMessage(
        int id,
        long peerId,
        MessageMedia media
    ) =>
        new()
        {
            message = new TL.Message
            {
                id = id,
                message = "мем",
                // Сущности обязательны: без них проверка ссылок падает, а текст без
                // разметки ссылок не содержит.
                entities = [],
                peer_id = new PeerChat { chat_id = peerId },
                media = media,
            },
        };

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

    /// <summary>
    /// Обработчик обновления запускается фоновой задачей, поэтому проверяется по
    /// появлению события, а не по ожиданию: сразу после вызова счётчик ещё нулевой.
    /// </summary>
    private async Task HandleAsync(RandomMemOnline service, UpdateNewChannelMessage update)
    {
        await InvokeAsync(service, "OnUpdate", [update]);

        for (var attempt = 0; attempt < 100; attempt++)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
            if (_queued.Count > 0)
            {
                return;
            }
        }
    }

    private static Task InvokeAsync(object service, string name, object?[] arguments) =>
        (Task)
            service
                .GetType()
                .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(service, arguments)!;

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

    private sealed class StubFactory(IOnlineTelegramClient client) : IOnlineTelegramClientFactory
    {
        public Task<IOnlineTelegramClient> CreateAsync(
            CancellationToken cancellationToken = default
        ) => Task.FromResult(client);
    }

    private sealed class FakeClient : IOnlineTelegramClient
    {
        public List<Func<UpdatesBase, Task>> Handlers { get; } = [];
        public bool Disposed { get; private set; }

        public event Func<UpdatesBase, Task> OnUpdates
        {
            add => Handlers.Add(value);
            remove => Handlers.Remove(value);
        }

        public Task RaiseAsync(Updates updates) =>
            Task.WhenAll(Handlers.Select(handler => handler(updates)));

        public Task<string> DownloadFileAsync(Document document, Stream output) =>
            Task.FromResult("video/mp4");

        public Task<string> DownloadFileAsync(Photo photo, Stream output, PhotoSize size) =>
            Task.FromResult("jpeg");

        public void Dispose() => Disposed = true;
    }
}
