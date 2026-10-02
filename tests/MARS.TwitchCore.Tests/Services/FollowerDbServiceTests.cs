using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.TwitchFollowers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Хранилище фоловеров поверх настоящего контекста в памяти.
///
/// Проверяется не «метод вернул», а данные: фоловер читается обратно с тем же
/// id, обновление не плодит дубликаты, а выборки для обновления и аватарок
/// отбирают именно тех, кто подходит под условие.
/// </summary>
public class FollowerDbServiceTests
{
    private readonly TwitchTestDbContextFactory _factory = new();
    private readonly FakeUserEnsureService _ensure;
    private readonly FollowerDbService _service;

    public FollowerDbServiceTests()
    {
        _ensure = new FakeUserEnsureService(_factory);
        _service = new FollowerDbService(_factory, _ensure, NullLogger<FollowerDbService>.Instance);
    }

    [Fact]
    public async Task SavedFollowerIsReadBack()
    {
        Assert.True(await _service.SaveOrUpdateFollowerAsync(Follower("123456789")));

        var stored = await _service.GetFollowerFromDbAsync("123456789");

        Assert.NotNull(stored);
        Assert.Equal("123456789", stored!.UserId);
    }

    /// <summary>
    /// Повторное сохранение обновляет запись, а не добавляет вторую: иначе на
    /// каждом обновлении фолловеров таблица росла бы вдвое.
    /// </summary>
    [Fact]
    public async Task RepeatedSaveUpdatesInsteadOfDuplicating()
    {
        await _service.SaveOrUpdateFollowerAsync(Follower("123456789"));

        Assert.True(await _service.SaveOrUpdateFollowerAsync(Follower("123456789")));
        Assert.Equal(1, await _service.GetFollowersCountAsync());
    }

    [Fact]
    public async Task SaveRequiresUserId()
    {
        Assert.False(await _service.SaveOrUpdateFollowerAsync(null));
        Assert.False(await _service.SaveOrUpdateFollowerAsync(Follower("  ")));
    }

    /// <summary>
    /// Пользователь должен появиться в базе раньше фоловера: фоловер ссылается на
    /// него внешним ключом, и без записи сохранение падало бы.
    /// </summary>
    [Fact]
    public async Task SaveEnsuresTwitchUserExists()
    {
        var user = TwitchUserOf("123456789", "login");

        await _service.SaveOrUpdateFollowerAsync(Follower("123456789", user));

        await using var context = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        var stored = await context.TwitchUsers.SingleAsync(
            item => item.TwitchId == "123456789",
            TestContext.Current.CancellationToken
        );

        Assert.Equal("login", stored.UserLogin);
    }

    [Fact]
    public async Task SaveEnsuresUserByIdWhenEntityIsAbsent()
    {
        await _service.SaveOrUpdateFollowerAsync(Follower("123456789"));

        await using var context = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );

        Assert.True(
            await context.TwitchUsers.AnyAsync(
                item => item.TwitchId == "123456789",
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task UnknownFollowerIsNotFound()
    {
        Assert.Null(await _service.GetFollowerFromDbAsync("987654321"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankIdIsNotFound(string userId)
    {
        Assert.Null(await _service.GetFollowerFromDbAsync(userId));
    }

    [Fact]
    public async Task AllFollowersAreListed()
    {
        await _service.SaveOrUpdateFollowerAsync(Follower("111111111"));
        await _service.SaveOrUpdateFollowerAsync(Follower("222222222"));

        Assert.Equal(2, (await _service.GetAllFollowersFromDbAsync()).Count);
    }

    [Fact]
    public async Task BatchSaveStoresEveryFollower()
    {
        var saved = await _service.SaveOrUpdateFollowersAsync([
            Follower("111111111"),
            Follower("222222222"),
            Follower("333333333"),
        ]);

        Assert.Equal(3, saved);
        Assert.Equal(3, await _service.GetFollowersCountAsync());
    }

    [Fact]
    public async Task BatchSaveUpdatesExistingFollowers()
    {
        await _service.SaveOrUpdateFollowerAsync(Follower("111111111"));

        var saved = await _service.SaveOrUpdateFollowersAsync([
            Follower("111111111"),
            Follower("222222222"),
        ]);

        Assert.Equal(2, saved);
        Assert.Equal(2, await _service.GetFollowersCountAsync());
    }

    [Fact]
    public async Task BatchSaveOfNothingChangesNothing()
    {
        Assert.Equal(0, await _service.SaveOrUpdateFollowersAsync(null));
        Assert.Equal(0, await _service.SaveOrUpdateFollowersAsync([]));
    }

    [Fact]
    public async Task FollowerIsDeleted()
    {
        await _service.SaveOrUpdateFollowerAsync(Follower("123456789"));

        Assert.True(await _service.DeleteFollowerAsync("123456789"));
        Assert.Equal(0, await _service.GetFollowersCountAsync());
    }

    [Fact]
    public async Task DeletingUnknownFollowerReportsFailure()
    {
        Assert.False(await _service.DeleteFollowerAsync("987654321"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeletingWithoutIdReportsFailure(string userId)
    {
        Assert.False(await _service.DeleteFollowerAsync(userId));
    }

    [Fact]
    public async Task BatchDeleteRemovesOnlyRequestedFollowers()
    {
        await _service.SaveOrUpdateFollowerAsync(Follower("111111111"));
        await _service.SaveOrUpdateFollowerAsync(Follower("222222222"));
        await _service.SaveOrUpdateFollowerAsync(Follower("333333333"));

        var deleted = await _service.DeleteFollowersAsync(["111111111", "333333333"]);

        Assert.Equal(2, deleted);
        Assert.Equal(1, await _service.GetFollowersCountAsync());
    }

    [Fact]
    public async Task BatchDeleteOfNothingChangesNothing()
    {
        await _service.SaveOrUpdateFollowerAsync(Follower("111111111"));

        Assert.Equal(0, await _service.DeleteFollowersAsync([]));
        Assert.Equal(0, await _service.DeleteFollowersAsync(null!));
        Assert.Equal(1, await _service.GetFollowersCountAsync());
    }

    [Fact]
    public async Task ClearRemovesEverythingAndReportsCount()
    {
        await _service.SaveOrUpdateFollowerAsync(Follower("111111111"));
        await _service.SaveOrUpdateFollowerAsync(Follower("222222222"));

        Assert.Equal(2, await _service.ClearAllFollowersAsync());
        Assert.Equal(0, await _service.GetFollowersCountAsync());
    }

    /// <summary>
    /// На обновление попадают только те, чей пользователь обновлялся раньше
    /// отсечки: остальные уже актуальны.
    /// </summary>
    [Fact]
    public async Task OutdatedFollowersAreSelectedForUpdate()
    {
        await SeedUserAsync("111111111", lastUpdated: DateTime.Now.AddDays(-2));
        await SeedUserAsync("222222222", lastUpdated: DateTime.Now);

        var outdated = await _service.GetFollowersToUpdateAsync(DateTime.Now.AddDays(-1));

        Assert.Equal(["111111111"], outdated);
    }

    [Fact]
    public async Task FollowersWithoutAvatarsAreListed()
    {
        await SeedUserAsync("111111111", DateTime.Now, avatar: null);
        await SeedUserAsync("222222222", DateTime.Now, avatar: "https://example.org/avatar.png");

        var withoutAvatars = await _service.GetUsersWithoutAvatarsAsync();

        Assert.Equal(["111111111"], withoutAvatars.Select(follower => follower.UserId));
        Assert.Equal(1, await _service.GetUsersWithoutAvatarsCountAsync());
    }

    /// <summary>
    /// Упавшая база не должна ронять чтение: подписчики получают пустой список и
    /// продолжают работать, вместо того чтобы валить фонную синхронизацию.
    /// </summary>
    [Fact]
    public async Task DatabaseFailureYieldsEmptyResults()
    {
        var factory = new Mock<IDbContextFactory<TwitchDbContext>>();
        factory
            .Setup(instance => instance.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));
        var service = new FollowerDbService(
            factory.Object,
            new FakeUserEnsureService(new TwitchTestDbContextFactory()),
            NullLogger<FollowerDbService>.Instance
        );

        Assert.Empty(await service.GetAllFollowersFromDbAsync());
        Assert.Empty(await service.GetFollowersToUpdateAsync(DateTime.Now));
        Assert.Empty(await service.GetUsersWithoutAvatarsAsync());
        Assert.Equal(0, await service.GetFollowersCountAsync());
        Assert.Equal(0, await service.GetUsersWithoutAvatarsCountAsync());
        Assert.Equal(0, await service.ClearAllFollowersAsync());
        Assert.Null(await service.GetFollowerFromDbAsync("123456789"));
        Assert.False(await service.DeleteFollowerAsync("123456789"));
        Assert.Equal(0, await service.DeleteFollowersAsync(["123456789"]));
        Assert.Equal(0, await service.SaveOrUpdateFollowersAsync([Follower("123456789")]));
        Assert.False(await service.SaveOrUpdateFollowerAsync(Follower("123456789")));
    }

    private async Task SeedUserAsync(
        string userId,
        DateTime lastUpdated,
        string? avatar = "https://example.org/a.png"
    )
    {
        await using var context = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        context.TwitchUsers.Add(
            new TwitchUser
            {
                TwitchId = userId,
                UserLogin = "login",
                DisplayName = "Pyro",
                ProfileImageUrl = avatar,
                LastUpdated = lastUpdated,
            }
        );
        context.FollowersEntitys.Add(new FollowerInfo { UserId = userId });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static FollowerInfo Follower(string userId, TwitchUser? user = null) =>
        new() { UserId = userId, TwitchUser = user };

    private static TwitchUser TwitchUserOf(string twitchId, string login) =>
        new()
        {
            TwitchId = twitchId,
            UserLogin = login,
            DisplayName = "Pyro",
        };
}
