using System.Text.Json;
using MARS.Alerts.Services.Alerts;
using MARS.Alerts.Services.TriggerWords;
using MARS.Shared.Configuration;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Media;
using MARS.Shared.Messaging;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Alerts.Tests;

/// <summary>
/// Показ алерта, когда зритель написал в чат ключевое слово.
/// </summary>
public class TriggerWordAlertDispatcherTests
{
    private static MediaInfo Alert(string triggerWord, bool isEnabled = true) =>
        new()
        {
            Id = Guid.NewGuid(),
            TextInfo = new MediaTextInfo { TriggerWord = triggerWord },
            FileInfo = new MediaFileInfo
            {
                Type = MediaType.Video,
                FilePath = "alert.mp4",
                FileName = "alert.mp4",
                Extension = ".mp4",
            },
            PositionInfo = new MediaPositionInfo(),
            MetaInfo = new MediaMetaInfo { DisplayName = "alert", IsEnabled = isEnabled },
            StylesInfo = new MediaStylesInfo(),
        };

    private static ChatMessageEvent Message(string text) =>
        new()
        {
            UserId = "42",
            UserName = "Viewer",
            Message = text,
            ChatColor = "#FF0000",
        };

    private static TriggerWordAlertDispatcher Build(
        IReadOnlyList<MediaInfo>? alerts,
        out Mock<ITelegramusNotifier> notifier
    )
    {
        var source = new Mock<IEnabledAlertSource>();
        source
            .Setup(x => x.GetEnabledAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(alerts);

        notifier = new Mock<ITelegramusNotifier>();
        notifier.Setup(x => x.Alert(It.IsAny<MediaDto>())).Returns(Task.CompletedTask);

        return new TriggerWordAlertDispatcher(
            source.Object,
            notifier.Object,
            NullLogger<TriggerWordAlertDispatcher>.Instance
        );
    }

    [Fact]
    public async Task DispatchAsync_ShowsAnAlert_ForAMatchingTriggerWord()
    {
        var alert = Alert("привет");
        var dispatcher = Build([alert], out var notifier);
        var ct = TestContext.Current.CancellationToken;

        await dispatcher.DispatchAsync(Message("всем привет"), ct);

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Once);
    }

    /// <summary>
    /// Монолит показывал ровно один случайный алерт из подходящих: shuffle и
    /// первый элемент. Показ всех сразу превращался бы в стену одинаковых
    /// картинок на каждое сообщение в чате.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_ShowsOnlyOneAlert_WhenSeveralMatch()
    {
        var alerts = new[] { Alert("привет"), Alert("приветики"), Alert("привет \"привет\"") };
        var dispatcher = Build(alerts, out var notifier);
        var ct = TestContext.Current.CancellationToken;

        await dispatcher.DispatchAsync(Message("приветики всем"), ct);

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Once);
    }

    /// <summary>
    /// Плейсхолдеры алерта подставляются по данным автора сообщения: монолит
    /// вызывал FixAlertText/FixAlertColor, поэтому в шаблоне работают
    /// {user.text}, {user.name} и {user.color}.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_FillsUserPlaceholdersInTheAlertText()
    {
        var alert = Alert("привет");
        alert.TextInfo.Text = "{user.name}: {user.text}";
        alert.TextInfo.TextColor = "{user.color}";
        var dispatcher = Build([alert], out var notifier);
        var ct = TestContext.Current.CancellationToken;

        var shown = new List<MediaDto>();
        notifier
            .Setup(x => x.Alert(It.IsAny<MediaDto>()))
            .Callback<MediaDto>(shown.Add)
            .Returns(Task.CompletedTask);

        await dispatcher.DispatchAsync(Message("привет"), ct);

        var media = Assert.Single(shown).MediaInfo;
        Assert.Equal("Viewer: привет", media.TextInfo.Text);
        Assert.Equal("#FF0000", media.TextInfo.TextColor);
    }

    [Fact]
    public async Task DispatchAsync_DoesNotShowAnything_ForAMessageWithoutTriggers()
    {
        var dispatcher = Build([Alert("привет")], out var notifier);
        var ct = TestContext.Current.CancellationToken;

        await dispatcher.DispatchAsync(Message("всем пока"), ct);

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_DoesNotShowAnything_WhenStorageIsUnavailable()
    {
        var dispatcher = Build(null, out var notifier);
        var ct = TestContext.Current.CancellationToken;

        await dispatcher.DispatchAsync(Message("привет"), ct);

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Never);
    }

    /// <summary>
    /// Формат сообщения берётся из <c>twitch.message.received</c>: потребитель
    /// обязан разобрать контракт, который публикует MARS.TwitchCore.
    /// </summary>
    [Fact]
    public async Task Consumer_ReadsTheChatMessageEventPublishedByTwitchCore()
    {
        var alert = Alert("привет");
        var source = new Mock<IEnabledAlertSource>();
        source
            .Setup(x => x.GetEnabledAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([alert]);

        var notifier = new Mock<ITelegramusNotifier>();
        notifier.Setup(x => x.Alert(It.IsAny<MediaDto>())).Returns(Task.CompletedTask);

        var dispatcher = new TriggerWordAlertDispatcher(
            source.Object,
            notifier.Object,
            NullLogger<TriggerWordAlertDispatcher>.Instance
        );

        var consumer = new TestableTriggerWordAlertConsumer(dispatcher);
        var ct = TestContext.Current.CancellationToken;
        var json = JsonSerializer.Serialize(Message("всем привет"));

        await consumer.Invoke(RabbitMqConfig.MessageReceived, json, ct);

        notifier.Verify(x => x.Alert(It.IsAny<MediaDto>()), Times.Once);
    }

    private sealed class TestableTriggerWordAlertConsumer(TriggerWordAlertDispatcher dispatcher)
        : TriggerWordAlertConsumer(
            Options.Create(new RabbitMqOptions()),
            dispatcher,
            NullLogger<TriggerWordAlertConsumer>.Instance
        )
    {
        public Task Invoke(string routingKey, string json, CancellationToken ct) =>
            HandleMessageAsync(routingKey, json, ct);
    }
}
