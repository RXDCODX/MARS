using MARS.CinemaQueue.Data;
using MARS.CinemaQueue.Entities;
using MARS.CinemaQueue.Interfaces;
using MARS.CinemaQueue.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.CinemaQueue.Tests.Services;

/// <summary>
/// Награда «добавить фильм в очередь».
///
/// Зритель выбирает фильм и получает его в очередь. Проверяется, что название
/// берётся из метаданных, а при их недоступности остаётся понятная подпись, и что
/// автор из известных зрителей помечается: по нему считается статистика.
/// </summary>
public class TwitchCinemaQueueServiceTests : IDisposable
{
    private readonly Mock<ICinemaQueueService> _queue = new();
    private readonly Mock<IMediaMetadataService> _metadata = new();
    private readonly TestDbContextFactory _factory = new();
    private readonly DbContextOptions<CinemaDbContext> _options;

    public TwitchCinemaQueueServiceTests()
    {
        _options = _factory.Options;
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task KnownViewerGetsMetadataTitle()
    {
        await SeedViewerAsync("123");
        _metadata
            .Setup(service =>
                service.GetMetadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new MediaMetadata { Title = "Начало", Description = "Сон" });
        QueueReturnsRequest();

        var result = await Create()
            .HandleCinemaQueueRedemptionAsync(
                "Pyro",
                "123",
                "https://www.kinopoisk.ru/film/301",
                "Фильм",
                Token
            );

        Assert.Equal("Начало", result!.Title);
        Assert.Equal("Сон", result.Description);
        Assert.Equal("123", result.TwitchUserId);
        Assert.Equal("https://www.kinopoisk.ru/film/301", result.MediaUrl);
    }

    /// <summary>
    /// Без метаданных запись всё равно попадает в очередь: подпись «запросил такой-то»
    /// лучше, чем отказ награды.
    /// </summary>
    [Fact]
    public async Task WithoutMetadataItemIsStillQueued()
    {
        await SeedViewerAsync("123");
        _metadata
            .Setup(service =>
                service.GetMetadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((MediaMetadata?)null);
        QueueReturnsRequest();

        var result = await Create()
            .HandleCinemaQueueRedemptionAsync(
                "Pyro",
                "123",
                "https://example.test/film",
                "Фильм",
                Token
            );

        Assert.Equal("Requested by Pyro", result!.Title);
        Assert.Contains("Фильм", result.Description);
    }

    /// <summary>
    /// Неизвестный зритель всё равно может заказать фильм, но без автора: иначе
    /// награда была бы недоступна новичкам.
    /// </summary>
    [Fact]
    public async Task UnknownViewerIsQueuedWithoutAuthor()
    {
        _metadata
            .Setup(service =>
                service.GetMetadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new MediaMetadata { Title = "Начало" });
        QueueReturnsRequest();

        var result = await Create()
            .HandleCinemaQueueRedemptionAsync(
                "Новичок",
                "999",
                "https://example.test/film",
                "Фильм",
                Token
            );

        Assert.Null(result!.TwitchUserId);
    }

    /// <summary>
    /// Без ссылки награда игнорируется: добавлять нечего.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyLinkIsIgnored(string? link)
    {
        var result = await Create()
            .HandleCinemaQueueRedemptionAsync("Pyro", "123", link!, "Фильм", Token);

        Assert.Null(result);
        Assert.Empty(_queue.Invocations);
    }

    /// <summary>
    /// Ошибка очереди не роняет обработчик награды: следующий запрос всё равно
    /// должен обработаться.
    /// </summary>
    [Fact]
    public async Task QueueFailureIsSurvived()
    {
        await SeedViewerAsync("123");
        _metadata
            .Setup(service =>
                service.GetMetadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new MediaMetadata { Title = "Начало" });
        _queue
            .Setup(service =>
                service.CreateMediaItemAsync(
                    It.IsAny<CreateMediaItemRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new InvalidOperationException("база недоступна"));

        var result = await Create()
            .HandleCinemaQueueRedemptionAsync(
                "Pyro",
                "123",
                "https://example.test/film",
                "Фильм",
                Token
            );

        Assert.Null(result);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task SeedViewerAsync(string twitchUserId)
    {
        await using var db = new CinemaDbContext(_options);
        db.CinemaQueue.Add(
            new CinemaMediaItem
            {
                MediaUrl = "https://example.test/seed",
                TwitchUserId = twitchUserId,
            }
        );
        await db.SaveChangesAsync(Token);
    }

    /// <summary>
    /// Очередь возвращает то, что ей передали: проверяется содержимое запроса,
    /// а не работа репозитория.
    /// </summary>
    private void QueueReturnsRequest() =>
        _queue
            .Setup(service =>
                service.CreateMediaItemAsync(
                    It.IsAny<CreateMediaItemRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (CreateMediaItemRequest request, CancellationToken _) =>
                    new CinemaMediaItemDto
                    {
                        Title = request.Title,
                        Description = request.Description,
                        MediaUrl = request.MediaUrl,
                        TwitchUserId = request.TwitchUserId,
                    }
            );

    private TwitchCinemaQueueService Create() =>
        new(
            _queue.Object,
            _metadata.Object,
            _factory,
            NullLogger<TwitchCinemaQueueService>.Instance
        );
}
