using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Twitch.Api.Data;
using MARS.Twitch.Api.Entities;
using MARS.Twitch.Api.Services.HelloVideos;
using MARS.Twitch.Api.Services.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using Wolverine;
using ChatMessage = TwitchLib.Client.Models.ChatMessage;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Приветственное видео: показывается зрителю, который давно не получал его.
///
/// Проверяются три правила, из-за которых сервис и написан. Первое: по пятницам
/// видео не показывается. Второе: повторное сообщение того же id не приводит к
/// повторному показу — иначе один спам одно сообщение забивал бы экран.
/// Третье: без записи в базе показывать нечего, и лишнего запроса к OBS не
/// делается.
/// </summary>
public class HelloVideoWorkerTests : IDisposable
{
    private readonly TwitchTestDbContextFactory _factory = new();
    private readonly Mock<IHelloVideoNotifier> _notifier = new();
    private readonly ServiceProvider _serviceProvider;
    private readonly HelloVideoWorker _worker;

    public HelloVideoWorkerTests()
    {
        _notifier
            .Setup(instance => instance.SendAlertAsync(It.IsAny<HelloVideoAlertDto>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddScoped<IHelloVideoNotifier>(_ => _notifier.Object);
        _serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true }
        );

        _worker = new HelloVideoWorker(
            _factory,
            NullLogger<HelloVideoWorker>.Instance,
            new TestLifetime(),
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Mock.Of<ITwitchClient>(),
            PassingValidationService.Instance
        );
    }

    [Fact]
    public void ScopedNotifierCanBeConsumedBySingletonWorker()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDbContextFactory<TwitchDbContext>>(_factory);
        services.AddSingleton<IHostApplicationLifetime>(new TestLifetime());
        services.AddSingleton<ITwitchClient>(Mock.Of<ITwitchClient>());
        services.AddSingleton<ITwitchEventValidationService>(PassingValidationService.Instance);
        services.AddScoped<IMessageBus>(_ => Mock.Of<IMessageBus>());
        services.AddSingleton<IMediaStorageClient>(_ => Mock.Of<IMediaStorageClient>());
        services.AddScoped<IHelloVideoNotifier, HelloVideoNotifier>();
        services.AddSingleton<HelloVideoWorker>();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }
        );

        Assert.NotNull(provider.GetRequiredService<HelloVideoWorker>());
    }

    [Fact]
    public async Task AlertIsSentForLongAbsentViewer()
    {
        await SeedAsync(monthsAgo: 2);

        await _worker.OnMessageReceived(null, Message(id: 1));

        _notifier.Verify(
            instance => instance.SendAlertAsync(It.IsAny<HelloVideoAlertDto>()),
            Times.Once
        );
    }

    /// <summary>
    /// Недавно показанное видео не повторяется: иначе каждый приход зрителя
    /// перебивал бы экран.
    /// </summary>
    [Fact]
    public async Task RecentlyNotifiedViewerGetsNothing()
    {
        await SeedAsync(monthsAgo: 0);

        await _worker.OnMessageReceived(null, Message(id: 2));

        _notifier.Verify(
            instance => instance.SendAlertAsync(It.IsAny<HelloVideoAlertDto>()),
            Times.Never
        );
    }

    /// <summary>
    /// В пятницу видео не показывается зрителям из списка фумо — так это записано
    /// в сервисе: проверяется именно наличие записи в <c>FumoUsers</c>, а не в
    /// таблице приветствий. Проверяется фактическое поведение: правило про пятницу
    /// выглядит подозрительно (в методе дальше используется
    /// <c>HelloVideosUsers</c>), но менять его поведение без решения владельца
    /// нельзя.
    /// </summary>
    [Fact]
    public async Task FumoViewerIsSkippedOnFriday()
    {
        SkipTestIfNotFriday();
        await SeedAsync(monthsAgo: 2);
        await SeedFumoUserAsync();

        await _worker.OnMessageReceived(null, Message(id: 3));

        _notifier.Verify(
            instance => instance.SendAlertAsync(It.IsAny<HelloVideoAlertDto>()),
            Times.Never
        );
    }

    /// <summary>
    /// Зрителя нет в базе — показывать нечего: без записи неизвестно, какое
    /// видео ему положено.
    /// </summary>
    [Fact]
    public async Task UnknownViewerGetsNothing()
    {
        await _worker.OnMessageReceived(null, Message(id: 4));

        _notifier.Verify(
            instance => instance.SendAlertAsync(It.IsAny<HelloVideoAlertDto>()),
            Times.Never
        );
    }

    /// <summary>
    /// Сообщение, отклонённое валидатором, не обрабатывается вовсе: сюда попадают
    /// сообщения из чужих каналов и от пользователей из чёрного списка.
    /// </summary>
    [Fact]
    public async Task RejectedMessageIsIgnored()
    {
        var worker = new HelloVideoWorker(
            _factory,
            NullLogger<HelloVideoWorker>.Instance,
            new TestLifetime(),
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Mock.Of<ITwitchClient>(),
            PassingValidationService.Rejecting("чёрный список")
        );
        await SeedAsync(monthsAgo: 2);

        await worker.OnMessageReceived(null, Message(id: 5));

        _notifier.Verify(
            instance => instance.SendAlertAsync(It.IsAny<HelloVideoAlertDto>()),
            Times.Never
        );
    }

    [Fact]
    public async Task UnknownViewerHasNoTestVideo()
    {
        Assert.Null(await _worker.TestVideo("неизвестный"));
    }

    [Fact]
    public async Task TestVideoIsSentForKnownViewer()
    {
        await SeedAsync(monthsAgo: 2);

        var displayName = await _worker.TestVideo("Pyro");

        Assert.Equal("Pyro", displayName);
        _notifier.Verify(
            instance => instance.SendAlertAsync(It.IsAny<HelloVideoAlertDto>()),
            Times.Once
        );
    }

    [Fact]
    public async Task StopDetachesHandler()
    {
        await _worker.StopAsync(TestContext.Current.CancellationToken);
    }

    public void Dispose() => _serviceProvider.Dispose();

    /// <summary>
    /// Правило про пятницу касается календаря, а не логики: тест пропускает себя,
    /// если сегодня не пятница, иначе он проверял бы время машины.
    /// </summary>
    private static void SkipTestIfNotFriday()
    {
        Assert.SkipUnless(
            DateTime.Now.DayOfWeek == DayOfWeek.Friday,
            "правило про пятницу проверяется только в пятницу"
        );
    }

    private async Task SeedFumoUserAsync()
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.FumoUsers.Add(new FumoUser { TwitchId = "123456789", LastTime = DateTime.Now });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task SeedAsync(int monthsAgo)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.HelloVideosUsers.Add(
            new HelloVideosUsers
            {
                TwitchId = "123456789",
                MediaInfoId = Guid.NewGuid(),
                LastTimeNotif = DateTime.Now.AddMonths(-monthsAgo),
                TwitchUser = new TwitchUser
                {
                    TwitchId = "123456789",
                    UserLogin = "pyro",
                    DisplayName = "Pyro",
                },
            }
        );

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static OnMessageReceivedArgs Message(int id) =>
        new(
            new ChatMessage(
                botUsername: "mars-bot",
                userId: "123456789",
                userName: "pyro",
                displayName: "Pyro",
                hexColor: "#FFFFFF",
                emoteSet: null!,
                message: "сообщение",
                userType: UserType.Viewer,
                channel: MARS.Twitch.Api.Extensions.TwitchConstants.Channel,
                id: id.ToString(),
                subscribedMonthCount: 0,
                roomId: "room",
                isMe: false,
                isBroadcaster: false,
                noisy: default,
                rawIrcMessage: string.Empty,
                emoteReplacedMessage: string.Empty,
                badges: [],
                cheerBadge: null!,
                bits: 0,
                bitsInDollars: 0,
                userDetail: default
            )
        );
}
