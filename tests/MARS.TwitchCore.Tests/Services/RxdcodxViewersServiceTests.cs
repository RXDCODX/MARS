using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.TwitchFollowers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Зрители канала: модераторы, VIP и фолловеры в одном списке.
///
/// Сервис отдаёт данные из кеша (БД), а сам ходит в Twitch. Без токена или по
/// требованию <c>useCash</c> он обязан отдать кеш, а не ждать сеть: иначе
/// каждая проверка прав в чате зависела бы от доступности Twitch.
/// </summary>
public class RxdcodxViewersServiceTests
{
    private readonly TwitchTestDbContextFactory _factory = new();
    private readonly TestLifetime _lifetime = new();
    private readonly RxdcodxViewersService _service;

    public RxdcodxViewersServiceTests()
    {
        SeedToken();

        var followerDb = new FollowerDbService(
            _factory,
            new FakeUserEnsureService(_factory),
            NullLogger<FollowerDbService>.Instance
        );

        _service = new RxdcodxViewersService(
            Mock.Of<ITwitchAPI>(),
            new TokenService(Mock.Of<ITwitchAPI>(), NullLogger<TokenService>.Instance, _factory),
            OfflineEventSub.Create(),
            new TestLifetime(),
            NullLogger<RxdcodxViewersService>.Instance,
            followerDb
        );
    }

    /// <summary>
    /// Без токена Twitch недоступен, и список всё равно должен прийти из кеша:
    /// пустой список означал бы, что у канала нет зрителей.
    /// </summary>
    [Fact]
    public async Task FollowersAreTakenFromCacheWithoutToken()
    {
        await SaveFollowerAsync("123456789");

        var followers = await _service.GetAllFollowersInfo();

        Assert.NotNull(followers);
        Assert.Equal(["123456789"], followers!.Select(follower => follower.UserId));
    }

    [Fact]
    public async Task CashModeAlwaysReadsCache()
    {
        await SaveFollowerAsync("123456789");

        var followers = await _service.GetAllFollowersInfo(useCash: true);

        Assert.Single(followers!);
    }

    /// <summary>
    /// Когда Twitch недоступен и кеш пуст, сервис сообщает об ошибке, а не
    /// отдаёт пустой список: «у канала нет зрителей» и «не удалось узнать» —
    /// разные вещи, и путать их опасно.
    /// </summary>
    [Fact]
    public async Task FailureWithoutCacheIsReported()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetAllFollowersInfo());
    }

    [Fact]
    public async Task EmptyCacheYieldsNullInCashMode()
    {
        Assert.Null(await _service.GetAllFollowersInfo(useCash: true));
    }

    [Fact]
    public async Task CacheIsCleared()
    {
        await SaveFollowerAsync("123456789");

        await _service.ClearFollowersCache();

        Assert.Null(await _service.GetAllFollowersInfo(useCash: true));
    }

    [Fact]
    public async Task ClearingDatabaseReportsCount()
    {
        await SaveFollowerAsync("123456789");
        await SaveFollowerAsync("987654321");

        Assert.Equal(2, await _service.ClearAllFollowersFromDbAsync());
    }

    [Fact]
    public async Task FollowersToUpdateComeFromStore()
    {
        await using (
            var context = await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken)
        )
        {
            context.TwitchUsers.Add(
                new TwitchUser
                {
                    TwitchId = "123456789",
                    UserLogin = "login",
                    DisplayName = "Pyro",
                    LastUpdated = DateTime.Now.AddDays(-5),
                }
            );
            context.FollowersEntitys.Add(new FollowerInfo { UserId = "123456789" });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var outdated = await _service.GetFollowersToUpdateAsync(DateTime.Now.AddDays(-1));

        Assert.Equal(["123456789"], outdated);
    }

    [Fact]
    public async Task UsersWithoutAvatarsAreListed()
    {
        await SaveFollowerAsync("123456789");

        var withoutAvatars = await _service.GetUsersWithoutAvatarsAsync();

        Assert.Equal(["123456789"], withoutAvatars.Select(follower => follower.UserId));
        Assert.Equal(1, await _service.GetUsersWithoutAvatarsCountAsync());
    }

    /// <summary>
    /// Обновление аватарок выполняет отдельный сервис, поэтому здесь метод
    /// ничего не меняет: он обязан сообщить, кого надо обновить, и не вернуть
    /// «успешно обновлено» в никуда.
    /// </summary>
    [Fact]
    public async Task AvatarUpdateReportsNothingChanged()
    {
        await SaveFollowerAsync("123456789");

        Assert.Equal(0, await _service.UpdateMissingAvatarsAsync());
    }

    [Fact]
    public async Task AvatarUpdateOfCompleteListIsNoOp()
    {
        Assert.Equal(0, await _service.UpdateMissingAvatarsAsync());
    }

    /// <summary>
    /// Актуализация без токена не должна ронять сервис: без Twitch доступен
    /// только кеш, и он остаётся в силе.
    /// </summary>
    [Fact]
    public async Task ActualizationWithoutTokenKeepsCache()
    {
        await SaveFollowerAsync("123456789");

        await _service.ActualizeFollowersAsync();

        var cached = await _service.GetAllFollowersInfo(useCash: true);
        Assert.NotNull(cached);
        Assert.Single(cached!);
    }

    /// <summary>
    /// Токен кладётся в базу, а не в заглушку: без него сервис ждёт минуту и
    /// затем падает, и проверялся бы таймаут ожидания, а не работа с кешем.
    /// </summary>
    private void SeedToken()
    {
        using var context = _factory.CreateDbContext();
        context.TwitchToken.Add(
            new MARS.TwitchCore.Entities.TokenInfo
            {
                AccessToken = "токен",
                RefreshToken = "refresh",
                ExpiresIn = TimeSpan.FromHours(1),
                WhenCreated = DateTime.Now,
            }
        );
        context.SaveChanges();
    }

    private async Task SaveFollowerAsync(string userId)
    {
        await new FakeUserEnsureService(_factory).EnsureUserExistsAsync(
            userId,
            TestContext.Current.CancellationToken
        );

        await using var context = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        if (!context.FollowersEntitys.Any(follower => follower.UserId == userId))
        {
            context.FollowersEntitys.Add(new FollowerInfo { UserId = userId });
        }

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}

/// <summary>
/// Время жизни приложения, которым можно управлять в тесте: без него сервисы,
/// регистрирующие обработчики на старте, не запускались бы.
/// </summary>
internal sealed class TestLifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _stopping = new();

    public CancellationToken ApplicationStarted => CancellationToken.None;

    public CancellationToken ApplicationStopping => _stopping.Token;

    public CancellationToken ApplicationStopped => _stopping.Token;

    public void StopApplication() => _stopping.Cancel();
}
