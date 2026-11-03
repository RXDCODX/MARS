using System.Text.Json;
using MARS.Alerts.Services.Alerts;
using MARS.Alerts.Services.RewardInput;
using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Shared.Configuration;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Alerts.Tests;

/// <summary>
/// Маршрутизация сообщений о наградах в диспетчер AC.S08.
/// </summary>
public class RewardInputConsumersTests
{
    private static MediaInfo Alert(Guid? twitchGuid = null, int pointsCost = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            TextInfo = new MediaTextInfo(),
            FileInfo = new MediaFileInfo
            {
                Type = MediaType.Video,
                FilePath = "alert.mp4",
                FileName = "alert.mp4",
                Extension = ".mp4",
            },
            PositionInfo = new MediaPositionInfo(),
            MetaInfo = new MediaMetaInfo
            {
                DisplayName = "alert",
                TwitchGuid = twitchGuid,
                TwitchPointsCost = pointsCost,
            },
            StylesInfo = new MediaStylesInfo(),
        };

    private static Mock<ITelegramusNotifier> Build(
        Mock<IEnabledAlertSource> source,
        params MediaInfo[] alerts
    )
    {
        source
            .Setup(x => x.GetEnabledAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(alerts);

        var notifier = new Mock<ITelegramusNotifier>();
        notifier.Setup(x => x.Alert(It.IsAny<MediaDto>())).Returns(Task.CompletedTask);

        return notifier;
    }

    private static RewardBoundAlertDispatcher Dispatcher(
        IEnabledAlertSource source,
        ITelegramusNotifier notifier
    ) =>
        new(
            source,
            notifier,
            new RickRollerService(notifier, new ConfigurationBuilder().Build()),
            NullLogger<RewardBoundAlertDispatcher>.Instance
        );

    [Fact]
    public async Task RewardInputMessageConsumer_ShowsTheAlertBoundToTheReward()
    {
        var rewardId = Guid.NewGuid();
        var source = new Mock<IEnabledAlertSource>();
        var notifier = Build(source, Alert(twitchGuid: rewardId));
        var consumer = new Probe(Dispatcher(source.Object, notifier.Object));
        var ct = TestContext.Current.CancellationToken;
        var json = JsonSerializer.Serialize(
            new ChatMessageEvent
            {
                UserId = "42",
                UserName = "Viewer",
                Message = "текст награды",
                CustomRewardId = rewardId.ToString(),
            }
        );

        await consumer.Invoke(RabbitMqConfig.RewardInputMessage, json, ct);

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Once);
    }

    /// <summary>
    /// На своей очереди потребитель не должен реагировать на обычное сообщение
    /// чата: это делает <c>TriggerWordAlertConsumer</c>.
    /// </summary>
    [Fact]
    public async Task RewardInputMessageConsumer_IgnoresPlainChatMessages()
    {
        var source = new Mock<IEnabledAlertSource>();
        var notifier = Build(source, Alert(twitchGuid: Guid.NewGuid()));
        var consumer = new Probe(Dispatcher(source.Object, notifier.Object));
        var ct = TestContext.Current.CancellationToken;
        var json = JsonSerializer.Serialize(
            new ChatMessageEvent
            {
                UserId = "42",
                UserName = "Viewer",
                Message = "привет",
            }
        );

        await consumer.Invoke(RabbitMqConfig.RewardInputMessage, json, ct);

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Never);
    }

    [Fact]
    public async Task CostMatchedRewardConsumer_ShowsTheAlertWithTheSameCost()
    {
        var source = new Mock<IEnabledAlertSource>();
        var notifier = Build(source, Alert(pointsCost: 500));
        var consumer = new CostProbe(Dispatcher(source.Object, notifier.Object));
        var ct = TestContext.Current.CancellationToken;
        var json = JsonSerializer.Serialize(
            new RewardRedeemedEvent
            {
                Cost = 500,
                UserName = "Viewer",
                UserId = "42",
                RewardId = "reward",
            }
        );

        await consumer.Invoke(RabbitMqConfig.RewardRedeemed, json, ct);

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Once);
    }

    /// <summary>
    /// Зонд открывает защищённый обработчик наружу: <c>RabbitMqConsumerBase</c>
    /// проверяется без брокера, тем же приёмом, что и остальные потребители.
    /// </summary>
    private sealed class Probe : RewardInputMessageConsumer
    {
        public Probe(RewardBoundAlertDispatcher dispatcher)
            : base(
                Options.Create(new RabbitMqOptions()),
                dispatcher,
                NullLogger<RewardInputMessageConsumer>.Instance
            ) { }

        public Task Invoke(string routingKey, string json, CancellationToken ct) =>
            HandleMessageAsync(routingKey, json, ct);
    }

    private sealed class CostProbe : CostMatchedRewardConsumer
    {
        public CostProbe(RewardBoundAlertDispatcher dispatcher)
            : base(
                Options.Create(new RabbitMqOptions()),
                dispatcher,
                NullLogger<CostMatchedRewardConsumer>.Instance
            ) { }

        public Task Invoke(string routingKey, string json, CancellationToken ct) =>
            HandleMessageAsync(routingKey, json, ct);
    }
}
