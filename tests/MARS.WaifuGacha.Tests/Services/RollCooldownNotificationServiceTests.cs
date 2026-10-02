using System.Reflection;
using System.Text.Json;
using MARS.Shared.Configuration;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using MARS.WaifuGacha.Entities;
using MARS.WaifuGacha.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.WaifuGacha.Tests.Services;

/// <summary>
/// Уведомления «кулдаун на ролл прошёл».
///
/// Сервис читает две шины: погашение награды (ставит кулдаун) и сообщение в чате
/// (проверяет, не истёк ли). Проверяются обе стороны и главное правило: уведомление
/// уходит в ответ пользователю, а не всем подряд, и только один раз за кулдаун.
///
/// Брокер не поднимается — обработчик вызывается напрямую, ровно так же, как его
/// зовёт <c>RabbitMqConsumerBase</c> по сообщению из очереди.
/// </summary>
public class RollCooldownNotificationServiceTests
{
    private readonly WaifuTestDbContextFactory _factory = new();
    private readonly Mock<IMarsEventBus> _bus = new();
    private readonly List<(string Key, object Message)> _published = [];
    private readonly RollCooldownNotificationService _service;

    public RollCooldownNotificationServiceTests()
    {
        _bus.Setup(instance =>
                instance.PublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns<string, object, CancellationToken>(
                (key, message, _) =>
                {
                    _published.Add((key, message));
                    return Task.CompletedTask;
                }
            );

        _service = new RollCooldownNotificationService(
            Options.Create(new RabbitMqOptions()),
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
        await HandleAsync(RabbitMqConfig.RewardRedeemed, Redeemed(cost: 1, title: "WaifuRoll"));

        Assert.Empty(await CooldownsAsync());
    }

    /// <summary>
    /// Награда не про ролл кулдаун не ставит: так антиспам не блокирует обычные
    /// награды канала.
    /// </summary>
    [Fact]
    public async Task ForeignRewardDoesNotStartCooldown()
    {
        await HandleAsync(RabbitMqConfig.RewardRedeemed, Redeemed(cost: 100, title: "Аяка"));

        Assert.Empty(await CooldownsAsync());
    }

    [Fact]
    public async Task RewardStartsCooldown()
    {
        await HandleAsync(RabbitMqConfig.RewardRedeemed, Redeemed(cost: 100, title: "WaifuRoll"));

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
        await HandleAsync(RabbitMqConfig.RewardRedeemed, Redeemed(cost: 100, title: title));

        Assert.Equal([expected], (await CooldownsAsync()).Select(cd => cd.RollType));
    }

    /// <summary>
    /// Событие без обязательных полей просто игнорируется: в шину попадают и
    /// события других сервисов, и разбирать их по частям нельзя.
    /// </summary>
    [Theory]
    [InlineData(RabbitMqConfig.RewardRedeemed, "null")]
    [InlineData(RabbitMqConfig.RewardRedeemed, "{}")]
    [InlineData(RabbitMqConfig.MessageReceived, "null")]
    [InlineData(RabbitMqConfig.MessageReceived, "{}")]
    public async Task EventsWithoutRequiredFieldsAreIgnored(string routingKey, string json)
    {
        await HandleAsync(routingKey, json);

        Assert.Empty(_published);
        Assert.Empty(await CooldownsAsync());
    }

    [Fact]
    public async Task UnknownRoutingKeyIsIgnored()
    {
        await HandleAsync("что-то другое", Redeemed(cost: 100, title: "WaifuRoll"));

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

        await HandleAsync(
            RabbitMqConfig.MessageReceived,
            JsonSerializer.Serialize(
                new ChatMessageEvent { UserId = "123456789", UserName = "Pyro" }
            )
        );

        var published = Assert.Single(_published);
        Assert.Equal(RabbitMqConfig.ChatSend, published.Key);
        var message = (ChatSendEvent)published.Message;
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

        await HandleAsync(
            RabbitMqConfig.MessageReceived,
            JsonSerializer.Serialize(
                new ChatMessageEvent { UserId = "другой", UserName = "Другой" }
            )
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
        await HandleAsync(RabbitMqConfig.RewardRedeemed, Redeemed(cost: 100, title: "WaifuRoll"));

        await HandleAsync(
            RabbitMqConfig.MessageReceived,
            JsonSerializer.Serialize(
                new ChatMessageEvent { UserId = "123456789", UserName = "Pyro" }
            )
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
        var pending = typeof(RollCooldownNotificationService)
            .GetField("_pendingNotifications", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(_service)!;

        pending.GetType().GetMethod("Add")!.Invoke(pending, [key, DateTime.UtcNow.AddMinutes(-1)]);
    }

    private async Task<List<RollCooldown>> CooldownsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );

        return await db.RollCooldowns.ToListAsync(TestContext.Current.CancellationToken);
    }

    private static string Redeemed(int cost, string title) =>
        JsonSerializer.Serialize(
            new MARS.Shared.Messaging.RewardRedeemedEvent
            {
                UserId = "123456789",
                UserName = "Pyro",
                RewardTitle = title,
                Cost = cost,
            }
        );

    private Task HandleAsync(string routingKey, string json) =>
        HandleAsync(_service, routingKey, json);

    private static async Task HandleAsync(
        RollCooldownNotificationService service,
        string routingKey,
        string json
    )
    {
        var method = typeof(RollCooldownNotificationService).GetMethod(
            "HandleMessageAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        await (Task)method.Invoke(service, [routingKey, json, CancellationToken.None])!;
    }
}
