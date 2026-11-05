using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Shared.Messaging;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Раздача reward-событий зарегистрированным обработчикам.
///
/// Все погашения приходят одним типом <see cref="RewardRedeemedEvent"/>, поэтому
/// выбор хендлера делается по ключу, выводимому из имени класса (или задаваемому
/// через keySelector), а не по RabbitMQ routing key. Событие чужой награды не
/// должно попадать к хендлеру другой награды — иначе поверх чужих алертов
/// появлялись бы лишние.
/// </summary>
public class RewardAlertDispatcherTests
{
    [Fact]
    public async Task HandlerReceivesItsReward()
    {
        var handler = new StubHandler("аяка");
        var dispatcher = Create(handler, h => ((StubHandler)h).Key);

        await dispatcher.DispatchAsync(Redeemed("Аяка"), TestContext.Current.CancellationToken);

        Assert.Equal(1, handler.Calls);
    }

    /// <summary>
    /// Событие без зарегистрированного обработчика просто игнорируется: не все
    /// награды что-то показывают, и это не ошибка.
    /// </summary>
    [Fact]
    public async Task UnknownRewardIsIgnored()
    {
        var handler = new StubHandler("аяка");
        var dispatcher = Create(handler, h => ((StubHandler)h).Key);

        await dispatcher.DispatchAsync(
            Redeemed("неизвестная"),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(0, handler.Calls);
    }

    /// <summary>
    /// При регистрации двух обработчиков с одним ключом работает первый: иначе
    /// событие показалось бы дважды.
    /// </summary>
    [Fact]
    public async Task FirstHandlerWinsForDuplicateRewardKey()
    {
        var key = "аяка";
        var first = new StubHandler(key);
        var second = new StubHandler(key);
        var dispatcher = Create(first, second, h => ((StubHandler)h).Key);

        await dispatcher.DispatchAsync(Redeemed("Аяка"), TestContext.Current.CancellationToken);

        Assert.Equal(1, first.Calls);
        Assert.Equal(0, second.Calls);
    }

    /// <summary>
    /// Название с пробелами и спецсимволами нормализуется: "Аяка!!!" → "аяка",
    /// и находит соответствующий хендлер.
    /// </summary>
    [Fact]
    public async Task RewardTitleIsNormalizedBeforeDispatch()
    {
        var handler = new StubHandler("аяка");
        var dispatcher = Create(handler, h => ((StubHandler)h).Key);

        await dispatcher.DispatchAsync(Redeemed("Аяка!!!"), TestContext.Current.CancellationToken);

        Assert.Equal(1, handler.Calls);
    }

    /// <summary>
    /// Награда, у которой нет обработчика, не является ошибкой: у неё может не
    /// быть визуального эффекта вовсе. Сообщение подтверждается, а не уходит в
    /// повтор и dead-letter очередь.
    /// </summary>
    [Fact]
    public async Task UnmatchedRewardDoesNotThrow()
    {
        var dispatcher = Create(new StubHandler("аяка"));

        await dispatcher.DispatchAsync(
            Redeemed("hello-video"),
            TestContext.Current.CancellationToken
        );
    }

    private static RewardRedeemedEvent Redeemed(string rewardTitle) =>
        new()
        {
            UserId = "123456789",
            RewardTitle = rewardTitle,
            Media = new MediaInfo
            {
                TextInfo = new MediaTextInfo { Text = "текст" },
                FileInfo = new MediaFileInfo
                {
                    Type = MediaType.Audio,
                    FilePath = "memory/аяка.mp3",
                    FileName = "аяка",
                    Extension = ".mp3",
                },
                PositionInfo = new MediaPositionInfo(),
                MetaInfo = new MediaMetaInfo { DisplayName = "Аяка" },
                StylesInfo = new MediaStylesInfo(),
            },
        };

    private static RewardAlertDispatcher Create(params IRewardAlertHandler[] handlers) =>
        new(handlers, NullLogger<RewardAlertDispatcher>.Instance);

    private static RewardAlertDispatcher Create(
        IRewardAlertHandler handler,
        Func<IRewardAlertHandler, string> keySelector
    ) => new([handler], NullLogger<RewardAlertDispatcher>.Instance, keySelector);

    private static RewardAlertDispatcher Create(
        IRewardAlertHandler first,
        IRewardAlertHandler second,
        Func<IRewardAlertHandler, string> keySelector
    ) => new([first, second], NullLogger<RewardAlertDispatcher>.Instance, keySelector);

    private sealed class StubHandler(string key) : IRewardAlertHandler
    {
        public string Key { get; } = key;

        public int Calls { get; private set; }

        public Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
        {
            Calls++;

            return Task.CompletedTask;
        }
    }
}
