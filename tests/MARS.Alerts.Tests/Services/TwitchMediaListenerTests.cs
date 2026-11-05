using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Shared.Grpc.Models;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using Moq;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Слушатель наград: превращает событие шины в оповещение оверлея.
///
/// Проверяется разведение потоков. Награда с медиа уходит в оверлей, а награда
/// без медиа — не уходит: иначе на экране появлялась бы пустая плашка на каждое
/// сообщение из чата.
/// </summary>
public class TwitchMediaListenerTests
{
    private readonly Mock<ITelegramusNotifier> _notifier = new();

    [Fact]
    public async Task RewardWithMediaIsForwarded()
    {
        var listener = Create();

        await listener.Handle(Redeemed(media: Media()), TestContext.Current.CancellationToken);

        _notifier.Verify(instance => instance.Alert(It.IsAny<MediaDto>()), Times.Once);
    }

    /// <summary>
    /// Награда без медиа не отправляется в оверлей: показывать нечего, и зритель
    /// увидел бы пустую плашку.
    /// </summary>
    [Fact]
    public async Task RewardWithoutMediaIsSkipped()
    {
        var listener = Create();

        await listener.Handle(Redeemed(media: null), TestContext.Current.CancellationToken);

        _notifier.Verify(instance => instance.Alert(It.IsAny<MediaDto>()), Times.Never);
    }

    /// <summary>
    /// Выключенный сервис молчит, даже когда награда пришла: переключатель
    /// <c>IsServiceActive</c> гасит оповещения без остановки подписки.
    /// </summary>
    [Fact]
    public async Task InactiveListenerIgnoresReward()
    {
        var listener = Create();
        listener.IsServiceActive = false;

        await listener.Handle(Redeemed(media: Media()), TestContext.Current.CancellationToken);

        _notifier.Verify(instance => instance.Alert(It.IsAny<MediaDto>()), Times.Never);
    }

    private TwitchMediaListener Create() => new(_notifier.Object);

    private static RewardRedeemedEvent Redeemed(MediaInfo? media) =>
        new()
        {
            UserId = "123456789",
            UserName = "Pyro",
            RewardTitle = "Аяка",
            Media = media,
        };

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
}
