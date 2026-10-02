using System.Reflection;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.TwitchFollowers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Фоновые обновления кеша зрителей.
///
/// Фоловеры, VIP и модераторы попадают в кеш из базы при старте и обновляются по
/// таймеру. Проверяется, что обновление при недоступном Twitch не ломает сервис и
/// не стирает накопленный кеш: иначе после кратковременной сбои сети модераторы
/// перестали бы проходить проверку прав в чате.
/// </summary>
public class RxdcodxViewersCacheTests
{
    private readonly TwitchTestDbContextFactory _factory = new();
    private readonly StartedLifetime _lifetime = new();

    public RxdcodxViewersCacheTests()
    {
        // Токен кладётся в базу: без него обращение к Twitch ждёт минуту и падает,
        // и проверялся бы таймаут ожидания, а не работа с кешем.
        using var context = _factory.CreateDbContext();
        context.TwitchToken.Add(
            new TokenInfo
            {
                AccessToken = "токен",
                RefreshToken = "refresh",
                ExpiresIn = TimeSpan.FromHours(1),
                WhenCreated = DateTime.Now,
            }
        );
        context.SaveChanges();
    }

    /// <summary>
    /// Принудительное обновление при недоступном Twitch не бросает исключение:
    /// команда администратора не должна ронять сервис.
    /// </summary>
    [Fact]
    public async Task ForcedRefreshSurvivesUnavailableTwitch()
    {
        await Create().RefreshFollowersCacheAsync();
    }

    /// <summary>
    /// Фоновое обновление по API при недоступном Twitch оставляет базу как есть: кеш
    /// не должен опустеть из-за сбоя сети.
    /// </summary>
    [Fact]
    public async Task BackgroundRefreshKeepsCache()
    {
        var service = Create();
        await SaveFollowerAsync("123456789");

        await InvokeAsync(service, "UpdateFollowersFromApiAsync");

        Assert.Single((await service.GetAllFollowersInfo(useCash: true))!);
    }

    /// <summary>
    /// Старт подписывает обработчики событий и поднимает кеш из базы. Проверяется,
    /// что это происходит по старту приложения, а не лениво: иначе первые минуты
    /// трансляции прошли бы со старым кешем.
    /// </summary>
    [Fact]
    public async Task StartupPreparesCacheFromDatabase()
    {
        var service = Create();
        await SaveFollowerAsync("123456789");

        await InvokeAsync(service, "ExecuteAsync", [TestContext.Current.CancellationToken]);
        await _lifetime.StartAsync();
        await WaitUntilAsync(async () =>
            (await service.GetAllFollowersInfo(useCash: true))?.Count == 1
        );
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        Assert.Fail("Фоновое обновление кеша не выполнилось");
    }

    private RxdcodxViewersService Create() =>
        new(
            Mock.Of<ITwitchAPI>(),
            new TokenService(Mock.Of<ITwitchAPI>(), NullLogger<TokenService>.Instance, _factory),
            OfflineEventSub.Create(),
            _lifetime,
            NullLogger<RxdcodxViewersService>.Instance,
            new FollowerDbService(
                _factory,
                new FakeUserEnsureService(_factory),
                NullLogger<FollowerDbService>.Instance
            )
        );

    private static Task InvokeAsync(
        RxdcodxViewersService service,
        string name,
        object?[]? arguments = null
    )
    {
        var method = typeof(RxdcodxViewersService).GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        return (Task)method.Invoke(service, arguments ?? [])!;
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
        context.FollowersEntitys.Add(new FollowerInfo { UserId = userId });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Старт приложения происходит по отмене токена: так регистрируются и
    /// подписки <see cref="IHostApplicationLifetime"/>.
    /// </summary>
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
}
