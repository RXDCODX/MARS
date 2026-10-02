using System.Reflection;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.TwitchFollowers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Interfaces;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Core.Models;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;

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

/// <summary>
/// События EventSub добавляют новых зрителей в кеш.
///
/// Фоловеры, VIP и модераторы приходят тремя разными событиями, и каждый должен
/// попасть в кеш: иначе человек, которого только что назначили модератором,
/// остался бы без прав в чате до конца стрима.
/// <summary>
/// События EventSub добавляют новых зрителей в кеш.
///
/// Фоловеры, VIP и модераторы приходят тремя разными событиями, и каждый должен
/// попасть в кеш: иначе человек, которого только что назначили модератором,
/// остался бы без прав в чате до конца стрима.
/// </summary>
public class RxdcodxEventHandlersTests
{
    private readonly TwitchTestDbContextFactory _factory = new();
    private readonly StartedLifetime _lifetime = new();

    /// <summary>
    /// Новый фоловер попадает в кеш сразу после события: иначе до обновления по
    /// таймеру человек считался бы чужим.
    /// </summary>
    [Fact]
    public async Task FollowEventAddsFollower()
    {
        var service = Create();

        await HandleAsync(
            service,
            "WsClientOnChannelFollow",
            new ChannelFollowArgs
            {
                Payload = new EventSubNotificationPayload<ChannelFollow>
                {
                    Event = new ChannelFollow
                    {
                        BroadcasterUserId = TwitchConstants.ChannelId,
                        UserId = "123456789",
                        UserName = "зритель",
                    },
                },
            }
        );

        Assert.Contains(
            (await service.GetAllFollowersInfo(useCash: true))!,
            follower => follower.UserId == "123456789"
        );
    }

    /// <summary>
    /// Событие чужого канала игнорируется: иначе кеш наполнялся бы фолловерами
    /// других стримов.
    /// </summary>
    [Fact]
    public async Task FollowEventOfForeignChannelIsIgnored()
    {
        var service = Create();

        await HandleAsync(
            service,
            "WsClientOnChannelFollow",
            new ChannelFollowArgs
            {
                Payload = new EventSubNotificationPayload<ChannelFollow>
                {
                    Event = new ChannelFollow
                    {
                        BroadcasterUserId = "other-channel",
                        UserId = "987654321",
                        UserName = "чужой",
                    },
                },
            }
        );

        // Кеш пуст — сервис возвращает null, а не пустой список: это разные состояния.
        Assert.True((await service.GetAllFollowersInfo(useCash: true)) is null);
    }

    /// <summary>
    /// Назначенный модератор попадает в кеш: без этого он не смог бы удалять
    /// сообщения в чате.
    /// </summary>
    [Fact]
    public async Task ModeratorEventAddsModerator()
    {
        var service = Create();

        await HandleAsync(
            service,
            "WsClientOnChannelModeratorAdd",
            new ChannelModeratorArgs
            {
                Payload = new EventSubNotificationPayload<ChannelModerator>
                {
                    Event = new ChannelModerator
                    {
                        BroadcasterUserId = TwitchConstants.ChannelId,
                        UserId = "555000111",
                        UserName = "модератор",
                    },
                },
            }
        );

        Assert.Contains(
            (await service.GetAllFollowersInfo(useCash: true))!,
            follower => follower.UserId == "555000111"
        );
    }

    /// <summary>
    /// Новый VIP попадает в кеш вместе с остальными зрителями: иначе ему не
    /// досталось бы эмоут в чате.
    /// </summary>
    [Fact]
    public async Task VipEventAddsVip()
    {
        var service = Create();

        await HandleAsync(
            service,
            "WsClientOnChannelVipAdd",
            new ChannelVipArgs
            {
                Payload = new EventSubNotificationPayload<ChannelVip>
                {
                    Event = new ChannelVip
                    {
                        BroadcasterUserId = TwitchConstants.ChannelId,
                        UserId = "777000222",
                        UserName = "vip",
                    },
                },
            }
        );

        Assert.Contains(
            (await service.GetAllFollowersInfo(useCash: true))!,
            follower => follower.UserId == "777000222"
        );
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

    private static async Task HandleAsync(RxdcodxViewersService service, string name, object args)
    {
        var method = typeof(RxdcodxViewersService).GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        await (Task)method.Invoke(service, [null, args])!;
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
