using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.UserSync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;
using ChatMessage = TwitchLib.Client.Models.ChatMessage;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Синхронизация пользователей по сообщениям в чате.
///
/// Сервис самый частый по числу вызовов: он срабатывает на каждое сообщение.
/// Проверяются три вещи, из-за которых он и написан: новый пользователь попадает в
/// базу, изменившиеся имя и права обновляются, а повторные сообщения того же
/// пользователя не дёргают базу чаще раза в пять минут.
/// </summary>
public class TwitchUserSyncServiceTests
{
    private readonly TwitchTestDbContextFactory _factory = new();
    private readonly TwitchUserSyncService _service;

    /// <summary>
    /// Фабрика одна на тестовый класс: у каждого экземпляра своя база в памяти,
    /// и сервис с чужой фабрикой писал бы в базу, которой не видно в проверках.
    /// </summary>
    public TwitchUserSyncServiceTests() => _service = Create(_factory);

    [Fact]
    public async Task NewChatUserIsStored()
    {
        await ReceivedAsync(Message());

        var user = await UserAsync("123456789");
        Assert.NotNull(user);
        Assert.Equal("login", user!.UserLogin);
        Assert.Equal("Pyro", user.DisplayName);
        Assert.Equal("#FFFFFF", user.ChatColor);
    }

    [Fact]
    public async Task ModeratorFlagIsTakenFromMessage()
    {
        await ReceivedAsync(Message(userDetail: new UserDetail(UserDetails.Moderator)));

        var user = await UserAsync("123456789");
        Assert.True(user!.IsModerator);
    }

    /// <summary>
    /// Смена отображаемого имени и логина в Twitch попадает в базу: иначе в
    /// панели и в командах остался бы прежний ник пользователя.
    /// </summary>
    [Fact]
    public async Task RenamedUserIsUpdated()
    {
        await SeedAsync("123456789", "старый логин", "Старое имя");

        await ReceivedAsync(Message());

        var user = await UserAsync("123456789");
        Assert.Equal("login", user!.UserLogin);
        Assert.Equal("Pyro", user.DisplayName);
    }

    /// <summary>
    /// Повторное сообщение в течение пяти минут игнорируется: иначе каждый зритель
    /// генерировал бы запись в базу на каждой реплике. Отсчёт идёт от первого
    /// обработанного сообщения — о времени последней записи в базе сервис не знает
    /// и после перезапуска обязан обновить данные заново.
    /// </summary>
    [Fact]
    public async Task RepeatedMessageWithinCooldownIsIgnored()
    {
        await SeedAsync("123456789", "login", "Старое имя");
        await ReceivedAsync(Message(displayName: "Первое имя"));
        var firstUpdated = (await UserAsync("123456789"))!.LastUpdated;

        await ReceivedAsync(Message(displayName: "Второе имя"));
        var user = await UserAsync("123456789");

        Assert.Equal("Первое имя", user!.DisplayName);
        Assert.Equal(firstUpdated, user.LastUpdated);
    }

    /// <summary>
    /// Сообщение отклонённого валидатором пользователя в базу не попадает: в том
    /// числе и чёрного списка — иначе синхронизация воскресила бы его данные.
    /// </summary>
    [Fact]
    public async Task RejectedMessageIsNotStored()
    {
        var service = new TwitchUserSyncService(
            Mock.Of<ITwitchClient>(),
            _factory,
            UserInfo(),
            TwitchTokenServiceStub.WithoutToken(),
            NullLogger<TwitchUserSyncService>.Instance,
            new TestLifetime(),
            PassingValidationService.Rejecting("пользователь в чёрном списке")
        );

        await ReceivedAsync(service, Message());

        Assert.Null(await UserAsync("123456789"));
    }

    [Fact]
    public async Task StartAndStopAreSafe()
    {
        await _service.StartAsync(TestContext.Current.CancellationToken);
        await _service.StopAsync(TestContext.Current.CancellationToken);
    }

    private static TwitchUserSyncService Create(TwitchTestDbContextFactory factory) =>
        new(
            Mock.Of<ITwitchClient>(),
            factory,
            UserInfo(),
            TwitchTokenServiceStub.WithoutToken(),
            NullLogger<TwitchUserSyncService>.Instance,
            new TestLifetime(),
            PassingValidationService.Instance
        );

    /// <summary>
    /// Обработчик сообщения приватный и подписан на клиент Twitch, поэтому
    /// вызывается напрямую — так же, как его зовёт IRC-клиент.
    /// </summary>
    private Task ReceivedAsync(OnMessageReceivedArgs args) => ReceivedAsync(_service, args);

    private static async Task ReceivedAsync(
        TwitchUserSyncService service,
        OnMessageReceivedArgs args
    )
    {
        var method = typeof(TwitchUserSyncService).GetMethod(
            "OnMessageReceived",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        )!;

        await (Task)method.Invoke(service, [null, args])!;
    }

    /// <summary>
    /// Сведения о пользователях — конкретный класс без конструктора без
    /// параметров, поэтому подменяется не мок, а настоящий экземпляр: без токена
    /// он всё равно не ходит в Twitch.
    /// </summary>
    private static TwitchUserInfoService UserInfo() =>
        new(
            Mock.Of<ITwitchAPI>(),
            TwitchTokenServiceStub.WithoutToken(),
            NullLogger<TwitchUserInfoService>.Instance
        );

    private static OnMessageReceivedArgs Message(
        string userId = "123456789",
        string displayName = "Pyro",
        UserDetail? userDetail = null
    ) =>
        new(
            new ChatMessage(
                botUsername: "mars-bot",
                userId: userId,
                userName: "login",
                displayName: displayName,
                hexColor: "#FFFFFF",
                emoteSet: null!,
                message: "текст",
                userType: UserType.Viewer,
                channel: MARS.TwitchCore.Extensions.TwitchConstants.Channel,
                id: "message-1",
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
                userDetail: userDetail ?? new UserDetail(UserDetails.None)
            )
        );

    private async Task<TwitchUser?> UserAsync(string twitchId)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );

        return await db.TwitchUsers.FirstOrDefaultAsync(
            user => user.TwitchId == twitchId,
            TestContext.Current.CancellationToken
        );
    }

    private async Task SeedAsync(string twitchId, string login, string displayName)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.TwitchUsers.Add(
            new TwitchUser
            {
                TwitchId = twitchId,
                UserLogin = login,
                DisplayName = displayName,
                LastUpdated = DateTime.Now,
            }
        );

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
