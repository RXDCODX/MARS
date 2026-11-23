using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
/// Проверяется листенер <see cref="RollCooldownChatListener"/>, который шлёт
/// уведомление в чат после истечения кулдауна. Кулдаун ставится REST-эндпоинтами
/// MARS.WaifuGacha (RollFrogFull/RollFumoFull/RollMikuFull/RollWaifuFull). Главное правило: уведомление
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

        using (var cleanup = _factory.CreateDbContext())
        {
            cleanup.RollCooldowns.RemoveRange(cleanup.RollCooldowns);
            cleanup.SaveChanges();
        }
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
        var cooldownConfiguration = new RollCooldownConfigurationService(
            _factory,
            NullLogger<RollCooldownConfigurationService>.Instance
        );
        var cooldown = await cooldownConfiguration.GetCooldownAsync(
            RootStateKeys.WaifuRollType,
            TestContext.Current.CancellationToken
        );

        await _state.AddPendingAsync(
            "123456789",
            RootStateKeys.WaifuRollType,
            cooldown,
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
