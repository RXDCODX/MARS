using System.Text.Json;
using MARS.Alerts.Application.Twitch.Rewards;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Раздача reward-событий зарегистрированным обработчикам.
///
/// Проверяется маршрутизация по названию награды: каждый обработчик отвечает за
/// свою награду, и событие чужой награды не должно попадать к нему — иначе поверх
/// чужих алертов появлялись бы лишние.
/// </summary>
public class RewardAlertConsumerTests
{
    [Fact]
    public async Task HandlerReceivesItsReward()
    {
        var handler = new StubHandler("аяка");
        var listener = Create(handler);

        await HandleAsync(listener, "Аяка");

        Assert.Equal(1, handler.Calls);
    }

    /// <summary>
    /// Событие без зарегистрированного обработчика просто игнорируется: не все
    /// награды что-то показывают.
    /// </summary>
    [Fact]
    public async Task UnknownRewardIsIgnored()
    {
        var handler = new StubHandler("аяка");
        var listener = Create(handler);

        await HandleAsync(listener, "Неизвестная");

        Assert.Equal(0, handler.Calls);
    }

    /// <summary>
    /// При регистрации двух обработчиков одной награды работает первый: иначе
    /// событие показалось бы дважды.
    /// </summary>
    [Fact]
    public async Task FirstHandlerWinsForDuplicateReward()
    {
        var first = new StubHandler("аяка");
        var second = new StubHandler("аяка");
        var listener = Create(first, second);

        await HandleAsync(listener, "Аяка");

        Assert.Equal(1, first.Calls);
        Assert.Equal(0, second.Calls);
    }

    [Fact]
    public void RewardKeyIsNormalizedTitle()
    {
        Assert.Equal("аяка", RewardRouting.KeyFor("Аяка"));
        Assert.Equal("phonkedit", RewardRouting.KeyFor("Phonk Edit"));
        Assert.Equal(string.Empty, RewardRouting.KeyFor("  "));
    }

    private static RewardAlertListener Create(params IRewardAlertHandler[] handlers) =>
        new(new RewardEventManager(handlers, NullLogger<RewardEventManager>.Instance));

    /// <summary>
    /// Обработчик вызывается напрямую, как это делает брокер по сообщению из
    /// очереди: поднимать RabbitMQ в тесте нельзя.
    /// </summary>
    private static async Task HandleAsync(RewardAlertListener listener, string rewardTitle)
    {
        await listener.Handle(
            new RewardRedeemedEvent
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
            },
            CancellationToken.None
        );
    }

    private sealed class StubHandler(string rewardKey) : IRewardAlertHandler
    {
        public string RewardKey { get; } = rewardKey;

        public int Calls { get; private set; }

        public Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
        {
            Calls++;

            return Task.CompletedTask;
        }
    }
}
