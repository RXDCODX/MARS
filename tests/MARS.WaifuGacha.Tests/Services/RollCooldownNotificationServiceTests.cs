using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MARS.Platform.Infrastructure.Grpc.Notifications;
using MARS.Shared.Contracts.Messaging;
using MARS.WaifuGacha.Domain.Entities;
using MARS.WaifuGacha.Infrastructure.Postgres.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Wolverine;

namespace MARS.WaifuGacha.Tests.Services;

/// <summary>
/// Уведомления «кулдаун на ролл прошёл».
///
/// Проверяются два листенера: <see cref="RollCooldownRewardListener"/> ставит
/// кулдаун при погашении награды, <see cref="RollCooldownChatListener"/> шлёт
/// уведомление в чат после истечения кулдауна. Главное правило: уведомление
/// уходит в ответ пользователю, а не всем подряд, и только один раз за кулдаун.
///
/// Брокер не поднимается — обработчики вызываются напрямую.
/// </summary>
public class RollCooldownNotificationServiceTests
{
    private readonly WaifuTestDbContextFactory _factory = new();
    private readonly Mock<IMessageBus> _bus = new();
    private readonly List<object> _published = [];
    private readonly RollCooldownNotificationState _state = new();

    public RollCooldownNotificationServiceTests()
    {
        _bus.Setup(instance =>
                instance.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions>())
            )
            .Returns(ValueTask.CompletedTask)
            .Callback((object message, DeliveryOptions? _) => _published.Add(message));
    }

    /// <summary>
    /// Награда дешевле стоимости ролла кулдаун не ставит: иначе трата копеечной
    /// награды блокировала бы ролл.
    /// </summary>
    [Fact]
    public async Task CheapRewardDoesNotStartCooldown()
    {
        var listener = new RollCooldownRewardListener(
            _state,
            new RollCooldownService(_factory, NullLogger<RollCooldownService>.Instance),
            new RollCooldownConfigurationService(
                _factory,
                NullLogger<RollCooldownConfigurationService>.Instance
            ),
            NullLogger<RollCooldownRewardListener>.Instance
        );

        await listener.Handle(
            Redeemed(cost: 1, title: "WaifuRoll"),
            TestContext.Current.CancellationToken
        );

        Assert.Empty(await CooldownsAsync());
        Assert.Empty(_published);
    }

    /// <summary>
    /// Награда не про ролл кулдаун не ставит: так антиспам не блокирует обычные
    /// награды канала.
    /// </summary>
    [Fact]
    public async Task ForeignRewardDoesNotStartCooldown()
    {
        var listener = new RollCooldownRewardListener(
            _state,
            new RollCooldownService(_factory, NullLogger<RollCooldownService>.Instance),
            new RollCooldownConfigurationService(
                _factory,
                NullLogger<RollCooldownConfigurationService>.Instance
            ),
            NullLogger<RollCooldownRewardListener>.Instance
        );

        await listener.Handle(
            Redeemed(cost: 100, title: "Аяка"),
            TestContext.Current.CancellationToken
        );

        Assert.Empty(await CooldownsAsync());
        Assert.Empty(_published);
    }

    [Fact]
    public async Task RewardStartsCooldown()
    {
        var listener = new RollCooldownRewardListener(
            _state,
            new RollCooldownService(_factory, NullLogger<RollCooldownService>.Instance),
            new RollCooldownConfigurationService(
                _factory,
                NullLogger<RollCooldownConfigurationService>.Instance
            ),
            NullLogger<RollCooldownRewardListener>.Instance
        );

        await listener.Handle(
            Redeemed(cost: 100, title: "WaifuRoll"),
            TestContext.Current.CancellationToken
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
        var listener = new RollCooldownRewardListener(
            _state,
            new RollCooldownService(_factory, NullLogger<RollCooldownService>.Instance),
            new RollCooldownConfigurationService(
                _factory,
                NullLogger<RollCooldownConfigurationService>.Instance
            ),
            NullLogger<RollCooldownRewardListener>.Instance
        );

        await listener.Handle(
            Redeemed(cost: 100, title: title),
            TestContext.Current.CancellationToken
        );

        Assert.Equal([expected], (await CooldownsAsync()).Select(cd => cd.RollType));
    }

    /// <summary>
    /// Событие без обязательных полей просто игнорируется.
    /// </summary>
    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task EventsWithoutRequiredFieldsAreIgnored(string json)
    {
        var listener = new RollCooldownRewardListener(
            _state,
            new RollCooldownService(_factory, NullLogger<RollCooldownService>.Instance),
            new RollCooldownConfigurationService(
                _factory,
                NullLogger<RollCooldownConfigurationService>.Instance
            ),
            NullLogger<RollCooldownRewardListener>.Instance
        );

        await listener.Handle(
            JsonSerializer.Deserialize<RewardRedeemedEvent>(json)!,
            TestContext.Current.CancellationToken
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
        await _state.AddPendingAsync(
            "123456789",
            RootStateKeys.WaifuRollType,
            TimeSpan.FromMinutes(-1),
            TestContext.Current.CancellationToken
        );

        var listener = new RollCooldownChatListener(
            _state,
            _bus.Object,
            NullLogger<RollCooldownChatListener>.Instance
        );

        await listener.Handle(
            new ChatEventMessageEvent { UserId = "123456789", UserName = "Pyro" },
            TestContext.Current.CancellationToken
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
        await _state.AddPendingAsync(
            "123456789",
            RootStateKeys.WaifuRollType,
            TimeSpan.FromMinutes(-1),
            TestContext.Current.CancellationToken
        );

        var listener = new RollCooldownChatListener(
            _state,
            _bus.Object,
            NullLogger<RollCooldownChatListener>.Instance
        );

        await listener.Handle(
            new ChatEventMessageEvent { UserId = "другой", UserName = "Другой" },
            TestContext.Current.CancellationToken
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
        var rewardListener = new RollCooldownRewardListener(
            _state,
            new RollCooldownService(_factory, NullLogger<RollCooldownService>.Instance),
            new RollCooldownConfigurationService(
                _factory,
                NullLogger<RollCooldownConfigurationService>.Instance
            ),
            NullLogger<RollCooldownRewardListener>.Instance
        );

        await rewardListener.Handle(
            Redeemed(cost: 100, title: "WaifuRoll"),
            TestContext.Current.CancellationToken
        );

        var chatListener = new RollCooldownChatListener(
            _state,
            _bus.Object,
            NullLogger<RollCooldownChatListener>.Instance
        );

        await chatListener.Handle(
            new ChatEventMessageEvent { UserId = "123456789", UserName = "Pyro" },
            TestContext.Current.CancellationToken
        );

        Assert.Empty(_published);
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
