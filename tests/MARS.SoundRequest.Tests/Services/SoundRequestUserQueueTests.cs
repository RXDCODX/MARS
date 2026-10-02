using MARS.SoundRequest.Data;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services;
using MARS.SoundRequest.Tests.Grpc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.SoundRequest.Tests.Services;

/// <summary>
/// Личная очередь зрителя.
///
/// Проверяется, что запросы одного зрителя не мешают чужим, а постановка в начало
/// сдвигает остальных: иначе «поиграй первым» перемешивал бы очередь и два трека
/// оказались бы на одной позиции.
/// </summary>
public class SoundRequestUserQueueTests
{
    private readonly TestDbContextFactory _factory;
    private readonly TestHostApplicationLifetime _lifetime = new();
    private readonly SoundRequestUserQueue _queue;

    public SoundRequestUserQueueTests()
    {
        _factory = new TestDbContextFactory(
            new DbContextOptionsBuilder<MediaDbContext>()
                .UseInMemoryDatabase($"sound-request-user-queue-{Guid.NewGuid():N}")
                .Options
        );
        _queue = new SoundRequestUserQueue(
            _factory,
            _lifetime,
            new StateManager(_factory, _lifetime, NullLogger<StateManager>.Instance)
        );
    }

    /// <summary>
    /// В начало очереди попадает тот, кто играет следующим: текущий трек остаётся
    /// на позиции 0, а новый встаёт сразу за ним.
    /// </summary>
    [Fact]
    public async Task TrackCanBePushedToTheFront()
    {
        await _queue.AddToQueueAsync(Track("первый"), "viewer", DateTime.UtcNow);

        var pushed = await _queue.AddToQueueFrontAsync(Track("первым"), "viewer");

        Assert.Equal("первым", pushed.Track!.TrackName);
        Assert.Equal(1, pushed.QueueOrder);
        Assert.Equal("первым", (await _queue.GetNextQueueItemAsync())!.Track!.TrackName);
    }

    /// <summary>
    /// Сдвиг очереди сохраняет порядок: прежний следующий уезжает за новым, а не
    /// теряется или получает ту же позицию.
    /// </summary>
    [Fact]
    public async Task PushingToFrontKeepsOrder()
    {
        await _queue.AddToQueueAsync(Track("первый"), "viewer", DateTime.UtcNow);
        await _queue.AddToQueueAsync(Track("второй"), "viewer", DateTime.UtcNow.AddSeconds(1));
        await _queue.AddToQueueAsync(Track("третий"), "viewer", DateTime.UtcNow.AddSeconds(2));

        await _queue.AddToQueueFrontAsync(Track("первым"), "viewer");

        Assert.Equal(
            ["первый", "первым", "второй", "третий"],
            (await _queue.GetQueueAsync())
                .OrderBy(item => item.QueueOrder)
                .Where(item => item.Track is not null)
                .Select(item => item.Track!.TrackName)
        );
    }

    /// <summary>
    /// Трек без автора не ставится в начало: иначе анонимный запрос вытеснял бы
    /// очередь.
    /// </summary>
    [Fact]
    public async Task AnonymousRequestIsNotPushed()
    {
        await _queue.AddToQueueAsync(Track("первый"), "viewer", DateTime.UtcNow);

        await _queue.AddToQueueFrontAsync(Track("аноним"), "  ");

        Assert.Single(await _queue.GetQueueAsync());
    }

    /// <summary>
    /// Следующий трек — тот, что стоит за текущим: иначе плеер после окончания
    /// начал бы играть уже то, что играет.
    /// </summary>
    [Fact]
    public async Task NextIsTheOneAfterCurrent()
    {
        await _queue.AddToQueueAsync(Track("первый"), "viewer", DateTime.UtcNow);
        await _queue.AddToQueueAsync(Track("второй"), "viewer", DateTime.UtcNow.AddSeconds(1));

        var next = await _queue.GetNextQueueItemAsync();

        Assert.Equal("второй", next!.Track!.TrackName);
    }

    /// <summary>
    /// Без очереди следующего трека нет: плеер должен замолчать, а не повторять
    /// текущий.
    /// </summary>
    [Fact]
    public async Task EmptyQueueHasNoNextTrack()
    {
        Assert.Null(await _queue.GetNextQueueItemAsync());
    }

    /// <summary>
    /// Зритель видит только свои запросы: чужие в его команде отмены выглядели бы
    /// как ошибка.
    /// </summary>
    [Fact]
    public async Task ViewerSeesOnlyOwnRequests()
    {
        await _queue.AddToQueueAsync(Track("мой"), "viewer", DateTime.UtcNow);
        await _queue.AddToQueueAsync(Track("чужой"), "other", DateTime.UtcNow.AddSeconds(1));

        var mine = await _queue.GetUserQueueItemsAsync("viewer");

        Assert.Equal(["мой"], mine.Select(item => item.Track!.TrackName));
    }

    /// <summary>
    /// Без идентификатора зрителя его запросы не ищутся: команда без автора не
    /// принадлежит никому.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task UnknownViewerHasNoItems(string? twitchId)
    {
        await _queue.AddToQueueAsync(Track("мой"), "viewer", DateTime.UtcNow);

        Assert.Empty(await _queue.GetUserQueueItemsAsync(twitchId!));
    }

    private static BaseTrackInfo Track(string name) =>
        new()
        {
            TrackName = name,
            Url = new Uri($"https://mars.example.org/{name}"),
            VideoId = Guid.NewGuid().ToString("N"),
            Duration = TimeSpan.FromMinutes(3),
        };
}
