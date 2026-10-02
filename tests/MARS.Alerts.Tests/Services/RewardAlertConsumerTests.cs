using System.Reflection;
using System.Text.Json;
using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Раздача reward-событий зарегистрированным обработчикам.
///
/// Проверяется маршрутизация по ключу награды: каждый обработчик отвечает за свою
/// награду, и событие чужой награды не должно попадать к нему — иначе поверх чужих
/// алертов появлялись бы лишние.
/// </summary>
public class RewardAlertConsumerTests
{
    [Fact]
    public async Task HandlerReceivesItsReward()
    {
        var handler = new StubHandler(RabbitMqConfig.RewardKey("аяка"));
        var consumer = Create(handler);

        await HandleAsync(consumer, RabbitMqConfig.RewardKey("аяка"));

        Assert.Equal(1, handler.Calls);
    }

    /// <summary>
    /// Событие без зарегистрированного обработчика просто игнорируется: не все
    /// награды что-то показывают.
    /// </summary>
    [Fact]
    public async Task UnknownRewardIsIgnored()
    {
        var handler = new StubHandler(RabbitMqConfig.RewardKey("аяка"));
        var consumer = Create(handler);

        await HandleAsync(consumer, RabbitMqConfig.RewardKey("неизвестная"));

        Assert.Equal(0, handler.Calls);
    }

    /// <summary>
    /// При регистрации двух обработчиков одной награды работает первый: иначе
    /// событие показалось бы дважды.
    /// </summary>
    [Fact]
    public async Task FirstHandlerWinsForDuplicateReward()
    {
        var key = RabbitMqConfig.RewardKey("аяка");
        var first = new StubHandler(key);
        var second = new StubHandler(key);
        var consumer = Create(first, second);

        await HandleAsync(consumer, key);

        Assert.Equal(1, first.Calls);
        Assert.Equal(0, second.Calls);
    }

    private static RewardAlertConsumer Create(params IRewardAlertHandler[] handlers) =>
        new(
            Options.Create(new RabbitMqOptions()),
            handlers,
            NullLogger<RewardAlertConsumer>.Instance
        );

    /// <summary>
    /// Обработчик вызывается напрямую, как это делает брокер по сообщению из
    /// очереди: поднимать RabbitMQ в тесте нельзя.
    /// </summary>
    private static async Task HandleAsync(RewardAlertConsumer consumer, string routingKey)
    {
        var method = typeof(RewardAlertConsumer).GetMethod(
            "HandleMessageAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        await (Task)
            method.Invoke(
                consumer,
                [
                    routingKey,
                    JsonSerializer.Serialize(
                        new RewardRedeemedEvent
                        {
                            UserId = "123456789",
                            RewardTitle = "Аяка",
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
                        }
                    ),
                    CancellationToken.None,
                ]
            )!;
    }

    private sealed class StubHandler(string routingKey) : IRewardAlertHandler
    {
        public string RoutingKey { get; } = routingKey;

        public int Calls { get; private set; }

        public Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
        {
            Calls++;

            return Task.CompletedTask;
        }
    }
}
