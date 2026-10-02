using MARS.CinemaQueue.Data;
using MARS.CinemaQueue.Entities;
using MARS.CinemaQueue.Interfaces;
using MARS.CinemaQueue.Repositories;
using MARS.CinemaQueue.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.CinemaQueue.Tests;

/// <summary>
/// Киноочередь: репозиторий на настоящей БД в памяти и сервис поверх него.
///
/// Репозиторий проверяется на настоящей схеме, а не на заглушке: в нём сортировка
/// по приоритету и времени создания, выбор «следующего» по флагу и пересчёт
/// флагов. На заглушке мы бы проверили, что тест сам же и заложил в неё.
/// </summary>
public class CinemaQueueServiceTests : IDisposable
{
    private readonly DbContextOptions<CinemaDbContext> _options;
    private readonly ICinemaQueueRepository _repository;
    private readonly CinemaQueueService _service;

    public CinemaQueueServiceTests()
    {
        _options = new DbContextOptionsBuilder<CinemaDbContext>()
            .UseInMemoryDatabase($"cinema-{Guid.NewGuid():N}")
            .Options;
        var factory = new TestDbContextFactory(_options);

        using (var database = new CinemaDbContext(_options))
        {
            database.Database.EnsureCreated();
        }

        _repository = new CinemaQueueRepository(factory);
        _service = new CinemaQueueService(_repository, NullLogger<CinemaQueueService>.Instance);
    }

    public void Dispose() => GC.SuppressFinalize(this);

    [Fact]
    public async Task CreatedItemIsPendingAndNotNext()
    {
        var created = await _service.CreateMediaItemAsync(
            new CreateMediaItemRequest
            {
                Title = "Довод",
                Description = "Описание",
                MediaUrl = "https://mars.example.org/movie",
                Priority = 10,
                TwitchUserId = "viewer",
                Notes = "заметка",
            },
            TestContext.Current.CancellationToken
        );

        Assert.Equal(MediaStatus.Pending, created.Status);
        Assert.False(created.IsNext);
        Assert.Equal("Довод", created.Title);
        Assert.Equal(10, created.Priority);
        Assert.Equal("viewer", created.TwitchUserId);
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async Task ItemsAreOrderedByPriority()
    {
        await AddAsync("низкий", priority: 1);
        await AddAsync("высокий", priority: 9);

        var all = (await _service.GetAllMediaItemsAsync(TestContext.Current.CancellationToken)).ToArray();

        Assert.Equal(["высокий", "низкий"], all.Select(item => item.Title));
    }

    [Fact]
    public async Task NextIsOnlyTheMarkedPendingItem()
    {
        var marked = await AddAsync("следующий", priority: 5);
        await AddAsync("обычный", priority: 1);

        Assert.Null(await _service.GetNextMediaItemAsync(TestContext.Current.CancellationToken));

        Assert.True(await _service.MarkAsNextAsync(marked.Id, TestContext.Current.CancellationToken));

        var next = await _service.GetNextMediaItemAsync(TestContext.Current.CancellationToken);
        Assert.Equal("следующий", next!.Title);
    }

    /// <summary>
    /// Пометка «следующим» снимается с предыдущего: два следующих элемента
    /// означали бы, что оверлей показывает оба и зритель не понимает, что
    /// включится.
    /// </summary>
    [Fact]
    public async Task MarkingNextClearsPreviousFlag()
    {
        var first = await AddAsync("первый", priority: 1);
        var second = await AddAsync("второй", priority: 2);

        await _service.MarkAsNextAsync(first.Id, TestContext.Current.CancellationToken);
        await _service.MarkAsNextAsync(second.Id, TestContext.Current.CancellationToken);

        var flags = await FlagsAsync();
        Assert.False(flags[first.Id]);
        Assert.True(flags[second.Id]);
    }

    [Fact]
    public async Task UpdateAppliesOnlyGivenFields()
    {
        var item = await AddAsync("до", priority: 3);

        var updated = await _service.UpdateMediaItemAsync(
            item.Id,
            new UpdateMediaItemRequest { Title = "после" },
            TestContext.Current.CancellationToken
        );

        Assert.Equal("после", updated!.Title);
        Assert.Equal("до", updated.Description);
        Assert.Equal(3, updated.Priority);
    }

    [Fact]
    public async Task UpdateAndReadOfMissingItemReturnNull()
    {
        Assert.Null(
            await _service.UpdateMediaItemAsync(
                Guid.NewGuid(),
                new UpdateMediaItemRequest { Title = "нет" },
                TestContext.Current.CancellationToken
            )
        );
        Assert.Null(
            await _service.GetMediaItemByIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task EmptyIdIsRejectedEverywhere()
    {
        Assert.Null(await _service.GetMediaItemByIdAsync(Guid.Empty, TestContext.Current.CancellationToken));
        Assert.Null(
            await _service.UpdateMediaItemAsync(
                Guid.Empty,
                new UpdateMediaItemRequest(),
                TestContext.Current.CancellationToken
            )
        );
        Assert.False(await _service.DeleteMediaItemAsync(Guid.Empty, TestContext.Current.CancellationToken));
        Assert.False(await _service.MarkAsNextAsync(Guid.Empty, TestContext.Current.CancellationToken));
        Assert.False(
            await _service.ChangeStatusAsync(
                Guid.Empty,
                MediaStatus.Completed,
                TestContext.Current.CancellationToken
            )
        );
        Assert.False(await _service.ChangePriorityAsync(Guid.Empty, 5, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StatusAndPriorityAreChanged()
    {
        var item = await AddAsync("фильм", priority: 1);

        Assert.True(
            await _service.ChangeStatusAsync(
                item.Id,
                MediaStatus.Completed,
                TestContext.Current.CancellationToken
            )
        );
        Assert.True(await _service.ChangePriorityAsync(item.Id, 42, TestContext.Current.CancellationToken));

        var updated = await _service.GetMediaItemByIdAsync(item.Id, TestContext.Current.CancellationToken);
        Assert.Equal(MediaStatus.Completed, updated!.Status);
        Assert.Equal(42, updated.Priority);
    }

    [Fact]
    public async Task MissingItemCannotBeChangedOrDeleted()
    {
        var missing = Guid.NewGuid();

        Assert.False(
            await _service.ChangeStatusAsync(
                missing,
                MediaStatus.Completed,
                TestContext.Current.CancellationToken
            )
        );
        Assert.False(await _service.ChangePriorityAsync(missing, 1, TestContext.Current.CancellationToken));
        Assert.False(await _service.MarkAsNextAsync(missing, TestContext.Current.CancellationToken));
        Assert.False(await _service.DeleteMediaItemAsync(missing, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ItemIsDeleted()
    {
        var item = await AddAsync("фильм", priority: 1);

        Assert.True(await _service.DeleteMediaItemAsync(item.Id, TestContext.Current.CancellationToken));

        Assert.Empty(await _service.GetAllMediaItemsAsync(TestContext.Current.CancellationToken));
        Assert.False(await _service.DeleteMediaItemAsync(item.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ItemsAreFilteredByStatus()
    {
        var pending = await AddAsync("в очереди", priority: 1);
        var completed = await AddAsync("показан", priority: 1);
        await _service.ChangeStatusAsync(
            completed.Id,
            MediaStatus.Completed,
            TestContext.Current.CancellationToken
        );

        var byStatus = (
            await _service.GetMediaItemsByStatusAsync(
                MediaStatus.Completed,
                TestContext.Current.CancellationToken
            )
        ).ToArray();

        Assert.Single(byStatus);
        Assert.Equal(completed.Id, byStatus[0].Id);
        Assert.NotEqual(pending.Id, byStatus[0].Id);
    }

    [Fact]
    public async Task StatisticsCountEveryStatusAndTotal()
    {
        await AddAsync("первый", priority: 1);
        var second = await AddAsync("второй", priority: 1);
        await _service.ChangeStatusAsync(
            second.Id,
            MediaStatus.Postponed,
            TestContext.Current.CancellationToken
        );

        var stats = await _service.GetStatisticsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, stats.PendingItems);
        Assert.Equal(1, stats.PostponedItems);
        Assert.Equal(0, stats.CompletedItems);
        Assert.Equal(2, stats.TotalItems);
    }

    private async Task<CinemaMediaItemDto> AddAsync(string title, int priority)
    {
        var created = await _service.CreateMediaItemAsync(
            new CreateMediaItemRequest
            {
                Title = title,
                Description = title,
                MediaUrl = $"https://mars.example.org/{title}",
                Priority = priority,
            },
            TestContext.Current.CancellationToken
        );

        var stored = await _repository.GetByIdAsync(created.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(stored);
        return created;
    }

    private async Task<Dictionary<Guid, bool>> FlagsAsync()
    {
        var flags = new Dictionary<Guid, bool>();

        await using var database = new CinemaDbContext(_options);
        foreach (var item in await database.CinemaQueue.ToListAsync(TestContext.Current.CancellationToken))
        {
            flags[item.Id] = item.IsNext;
        }

        return flags;
    }
}
