using System.Reflection;
using System.Text.Json;
using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Shared.Configuration;
using MARS.Shared.Grpc.Models;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Потребитель наград: превращает событие RabbitMQ в оповещение оверлея.
///
/// Проверяется разведение потоков. Награда с медиа уходит в оверлей, а награда без
/// медиа — не уходит: иначе на экране появлялась бы пустая плашка на каждое
/// сообщение из чата.
/// </summary>
public class TwitchMediaAlertsTests
{
    private readonly Mock<ITelegramusNotifier> _notifier = new();
    private readonly TwitchMediaAlerts _consumer;

    public TwitchMediaAlertsTests() =>
        _consumer = new TwitchMediaAlerts(
            Options.Create(new RabbitMqOptions()),
            _notifier.Object,
            NullLogger<TwitchMediaAlerts>.Instance
        );

    [Fact]
    public async Task RewardWithMediaIsForwarded()
    {
        await HandleAsync(Redeemed(media: Media()));

        _notifier.Verify(instance => instance.Alert(It.IsAny<MediaDto>()), Times.Once);
    }

    /// <summary>
    /// Награда без медиа не отправляется в оверлей: показывать нечего, и зритель
    /// увидел бы пустую плашку.
    /// </summary>
    [Fact]
    public async Task RewardWithoutMediaIsSkipped()
    {
        await HandleAsync(Redeemed(media: null));

        _notifier.Verify(instance => instance.Alert(It.IsAny<MediaDto>()), Times.Never);
    }

    /// <summary>
    /// Тело сообщения — JSON <c>null</c>: события нет, оповещение не отправляется.
    /// </summary>
    [Fact]
    public async Task NullPayloadIsSkipped()
    {
        await HandleAsync("null");

        _notifier.Verify(instance => instance.Alert(It.IsAny<MediaDto>()), Times.Never);
    }

    /// <summary>
    /// Битый JSON не доходит до оверлея и поднимается наружу: так base-класс
    /// отправляет сообщение в ретрай и, исчерпав попытки, в DLQ. Проверять надо
    /// именно это — проглотить исключение здесь означало бы потерю награды.
    /// </summary>
    [Fact]
    public async Task MalformedPayloadFailsBeforeOverlay()
    {
        var failure = await Assert.ThrowsAsync<JsonException>(() => HandleAsync("{ это не json"));

        Assert.NotNull(failure);
        _notifier.Verify(instance => instance.Alert(It.IsAny<MediaDto>()), Times.Never);
    }

    /// <summary>
    /// Выключенный сервис молчит, даже когда награда пришла: переключатель
    /// <c>IsServiceActive</c> гасит оповещения без остановки подписки.
    /// </summary>
    [Fact]
    public async Task InactiveConsumerIgnoresReward()
    {
        _consumer.IsServiceActive = false;

        await HandleAsync(Redeemed(media: Media()));

        _notifier.Verify(instance => instance.Alert(It.IsAny<MediaDto>()), Times.Never);
    }

    private static string Redeemed(MediaInfo? media) =>
        JsonSerializer.Serialize(
            new RewardRedeemedEvent
            {
                UserId = "123456789",
                UserName = "Pyro",
                RewardTitle = "Аяка",
                Media = media,
            }
        );

    private static MediaInfo Media() =>
        new()
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
        };

    /// <summary>
    /// Обработчик вызывается напрямую, как это делает брокер по сообщению из
    /// очереди: поднимать RabbitMQ в тесте нельзя.
    /// </summary>
    private Task HandleAsync(string json)
    {
        var method = typeof(TwitchMediaAlerts).GetMethod(
            "HandleMessageAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        return (Task)
            method.Invoke(
                _consumer,
                [RabbitMqConfig.RewardRedeemed, json, CancellationToken.None]
            )!;
    }
}
