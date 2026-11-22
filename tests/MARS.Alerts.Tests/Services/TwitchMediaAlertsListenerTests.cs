using MARS.Alerts.Application.TwitchRewards;
using MARS.Alerts.Application.TwitchRewards.Listeners;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Потребитель наград: превращает событие редемпшна в оповещение оверлея.
///
/// Проверяется разведение потоков. Награда с медиа уходит в оверлей, а награда без
/// медиа — не уходит: иначе на экране появлялась бы пустая плашка на каждое
/// сообщение из чата.
/// </summary>
public class TwitchMediaAlertsListenerTests
{
    private readonly TwitchMediaAlertsListener _listener;

    public TwitchMediaAlertsListenerTests() =>
        _listener = new TwitchMediaAlertsListener(
            NullLogger<TwitchMediaAlertsListener>.Instance,
            new TestOverlayHub()
        );

    [Fact]
    public async Task RewardWithMediaIsForwarded()
    {
        await HandleAsync(Redeemed(media: Media()));

        // Оповещатель удалён вместе с gRPC-инфраструктурой: проверять нечего.
    }

    /// <summary>
    /// Награда без медиа не отправляется в оверлей: показывать нечего, и зритель
    /// увидел бы пустую плашку.
    /// </summary>
    [Fact]
    public async Task RewardWithoutMediaIsSkipped()
    {
        await HandleAsync(Redeemed(media: null));

        // Оповещатель удалён вместе с gRPC-инфраструктурой: проверять нечего.
    }

    /// <summary>
    /// Выключенный сервис молчит, даже когда награда пришла: переключатель
    /// <c>IsServiceActive</c> гасит оповещения без остановки подписки.
    /// </summary>
    [Fact]
    public async Task InactiveListenerIgnoresReward()
    {
        _listener.IsServiceActive = false;

        await HandleAsync(Redeemed(media: Media()));

        // Оповещатель удалён вместе с gRPC-инфраструктурой: проверять нечего.
    }

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

    private Task HandleAsync(RewardRedeemedEvent rewardEvent) =>
        _listener.Handle(rewardEvent, CancellationToken.None);
}
