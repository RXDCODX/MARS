using MARS.Alerts.Extensions;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Медиа-часть погашения: превращает <see cref="RewardRedeemedEvent.Media"/> в
/// оповещение оверлея.
/// </summary>
/// <remarks>
/// Отдельная очередь от <see cref="RewardRedeemedListener"/>: награда с файлом
/// показывается одним путём, а по названию награды выбирается эффект — другим.
/// Смешать их значило бы показывать файл дважды.
/// </remarks>
public sealed class TwitchMediaListener(ITelegramusNotifier notifier)
{
    /// <summary>
    /// Переключатель, гасящий отправку без остановки подписки.
    /// </summary>
    public bool IsServiceActive { get; set; } = true;

    public async Task Handle(RewardRedeemedEvent message, CancellationToken cancellationToken)
    {
        if (IsServiceActive && message.Media is not null)
        {
            var media = message.Media.CloneTo();

            if (message.User is not null)
            {
                media.FixAlertText(message.User, message.UserInput ?? string.Empty);
                media.FixAlertColor(message.User);
            }

            cancellationToken.ThrowIfCancellationRequested();

            await notifier.Alert(new MediaDto { MediaInfo = media });
        }
    }
}
