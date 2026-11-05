using System.Reflection;
using System.Text.Json;
using MARS.Shared.Messaging;
using MARS.WaifuGacha.Entities;
using MARS.WaifuGacha.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Wolverine;

namespace MARS.WaifuGacha.Tests.Services;

/// <summary>
/// Уведомления «кулдаун на ролл прошёл».
///
/// Сервис читает два типа сообщений: погашение награды (ставит кулдаун) и
/// сообщение в чате (проверяет, не истёк ли). Проверяются обе стороны и
/// главное правило: уведомление уходит в ответ пользователю, а не всем
/// подряд, и только один раз за кулдаун.
///
/// Брокер не поднимается — методы сервиса вызываются напрямую, ровно так же,
/// как их вызывают слушатели RollCooldownListener и ChatCooldownListener.
/// </summary>
public class RollCooldownNotificationServiceTests
{
    private readonly WaifuTestDbContextFactory _factory = new();
    private readonly Mock<IMessageBus> _bus = new();
    private readonly List<object> _published = [];
    private readonly RollCooldownNotificationService _service;

    public RollCooldownNotificationServiceTests()
    {
        _bus.Setup(instance =>
                instance.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions>())
            )
            .Callback<object, DeliveryOptions>((m, _) => _published.Add(m))
            .Returns(ValueTask.CompletedTask);

        _service = new RollCooldownNotificationService(
            _bus.Object,
            new RollCooldownService(_factory, NullLogger<RollCooldownService>.Instance),
            new RollCooldownConfigurationService(
                _factory,
                NullLogger<RollCooldownConfigurationService>.Instance
            ),
            NullLogger<RollCooldownNotificationService>.Instance
        );
    }

    /// <summary>
    /// Награда дешевле стоимости ролла кулдаун не ставит: иначе трата копеечной
    /// награды блокировала бы ролл.
    /// </summary>
    [Fact]
    public async Task CheapRewardDoesNotStartCooldown()
    {
        await _service.OnRewardRedeemedAsync(
            Redeemed(cost: 1, title: "WaifuRoll"),
            CancellationToken.None
        );

        Assert.Empty(await CooldownsAsync());
    }

    /// <summary>
    /// Награда не про ролл кулдаун не ставит: так антиспам не блокирует обычные
    /// награды канала.
    /// </summary>
    [Fact]
    public async Task ForeignRewardDoesNotStartCooldown()
    {
        await _service.OnRewardRedeemedAsync(
            Redeemed(cost: 100, title: "Аяка"),
            CancellationToken.None
        );

        Assert.Empty(await CooldownsAsync());
    }

    [Fact]
    public async Task RewardStartsCooldown()
    {
        await _service.OnRewardRedeemedAsync(
            Redeemed(cost: 100, title: "WaifuRoll"),
            CancellationToken.None
        );

        var cooldowns = await CooldownsAsync();
        Assert.Equal([RootStateKeys.WaifuRollType], cooldowns.Select(cd => cd.RollType));
    }

    /// <summary>
    /// Тип ролла выводится из названия награды без учёта регистра и языка:
    /// «WAIFU ROLL» и «Вайфу ролл» защищаются так же, как «WaifuRoll».
    /// </summary>
    [Theory]
    [InlineData("WaifuRoll", RootStateKeys.WaifuRollType)]
    [InlineData("WAIFU ROLL", RootStateKeys.WaifuRollType)]
    [InlineData("Вайфу ролл", RootStateKeys.WaifuRollType)]
    [InlineData("FumoRoll", RootStateKeys.FumoRollType)]
    [InlineData("MikuRoll", RootStateKeys.MikuRollType)]
    [InlineData("Жаба ролл", RootStateKeys.FrogRollType)]
    public async Task RollTypeIsResolvedFromRewardTitle(string title, string expected)
    {
        await _service.OnRewardRedeemedAsync(
            Redeemed(cost: 100, title: title),
            CancellationToken.None
        );

        Assert.Equal([expected], (await CooldownsAsync()).Select(cd => cd.RollType));
    }

    /// <summary>
    /// Событие без обязательных полей просто игнорируется: в шину попадают и
    /// события других сервисов, и разбирать их по частям нельзя.
    /// </summary>
    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task EventsWithoutRequiredFieldsAreIgnored(string json)
    {
        await _service.OnRewardRedeemedAsync(
            JsonSerializer.Deserialize<RewardRedeemedEvent>(json)!,
            CancellationToken.None
        );

        Assert.Empty(_published);
        Assert.Empty(await CooldownsAsync());
    }

    /// <summary>
    /// Истёкший кулдаун приводит к сообщению в чат с обращением по нику: именно
    /// это сообщение зритель и ждёт после ролла.
    /// </summary>
    [Fact]
    public async Task ExpiredCooldownNotifiesUser()
    {
        ExpirePending(("123456789", RootStateKeys.WaifuRollType));

        await _service.OnChatMessageAsync(
            new ChatMessageReceivedEvent { UserId = "123456789", UserName = "Pyro" },
            CancellationToken.None
        );

        var published = Assert.Single(_published);
        var message = Assert.IsType<ChatSendEvent>(published);
        // ReplyTo — id, по которому сервис платформ отвечает именно этому
        // пользователю, а в тексте обращение идёт по нику.
        Assert.Equal("123456789", message.ReplyTo);
        Assert.Contains("@Pyro", message.Message);
        Assert.Contains("кулдаун", message.Message);
        // Канал основной, а не тот, откуда пришло сообщение: ответ должен быть
        // виден всем зрителям. Константа продублирована намеренно — проект
        // WaifuGacha не ссылается на TwitchCore.
        Assert.Equal("rxdcodx", message.Channel);
    }

    /// <summary>
    /// Сообщение от другого пользователя не вызывает уведомление: иначе первый
    /// же зритель в чате рассылал бы всем «ваш кулдаун прошёл».
    /// </summary>
    [Fact]
    public async Task MessageOfAnotherUserDoesNotNotify()
    {
        ExpirePending(("123456789", RootStateKeys.WaifuRollType));

        await _service.OnChatMessageAsync(
            new ChatMessageReceivedEvent { UserId = "другой", UserName = "Другой" },
            CancellationToken.None
        );

        Assert.Empty(_published);
    }

    /// <summary>
    /// Не истёкший кулдаун уведомления не вызывает: сообщения в чате есть
    /// постоянно, и уведомление сыпалось бы на каждом.
    /// </summary>
    [Fact]
    public async Task PendingCooldownDoesNotNotifyYet()
    {
        await _service.OnRewardRedeemedAsync(
            Redeemed(cost: 100, title: "WaifuRoll"),
            CancellationToken.None
        );

        await _service.OnChatMessageAsync(
            new ChatMessageReceivedEvent { UserId = "123456789", UserName = "Pyro" },
            CancellationToken.None
        );

        Assert.Empty(_published);
    }

    /// <summary>
    /// Момент окончания кулдауна ставится по <c>UtcNow</c> плюс кулдаун из
    /// конфигурации, поэтому подделать его иначе нечем: запись кладётся в
    /// приватное поле так же, как её кладёт обработчик награды.
    /// </summary>
    private void ExpirePending((string UserId, string RollType) key)
    {
        var pending = typeof(RollCooldownNotificationService).GetField(
            "_pendingNotifications",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;
        var dictionary = (Dictionary<(string, string), DateTime>)pending.GetValue(_service)!;

        dictionary.Add(key, DateTime.UtcNow.AddMinutes(-1));
    }

    private async Task<List<RollCooldown>> CooldownsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );

        return await db.RollCooldowns.ToListAsync(TestContext.Current.CancellationToken);
    }

    private static RewardRedeemedEvent Redeemed(int cost, string title) =>
        new()
        {
            UserId = "123456789",
            UserName = "Pyro",
            RewardTitle = title,
            Cost = cost,
        };
}
