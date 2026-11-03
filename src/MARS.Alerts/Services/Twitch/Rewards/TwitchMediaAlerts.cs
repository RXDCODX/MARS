using MARS.Alerts.Extensions;
using MARS.Shared.Configuration;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Потребитель общего потока <c>twitch.reward.#</c>: превращает
/// <see cref="RewardRedeemedEvent"/> в gRPC-событие для источника OBS.
/// Раньше тело сообщения десериализовалось в <c>MediaInfo</c>, тогда как
/// публикующая сторона клала <c>RewardRedeemedEvent</c> — десериализация всегда
/// возвращала <c>null</c>, и каждое сообщение уходило в ветку
/// "payload without valid MediaInfo" (блокер №5 из аудита).
/// Теперь типы согласованы: медиа лежит в <see cref="RewardRedeemedEvent.Media"/>.
/// </summary>
public class TwitchMediaAlerts(
    IOptions<RabbitMqOptions> options,
    ITelegramusNotifier notifier,
    ILogger<TwitchMediaAlerts> logger
)
    : RabbitMqConsumerBase(
        options.Value,
        "alerts",
        RabbitMqConfig.AlertsQueue,
        [RabbitMqConfig.RewardEventsPattern],
        logger
    )
{
    public bool IsServiceActive { get; set; } = true;

    protected override async Task HandleMessageAsync(
        string routingKey,
        string json,
        CancellationToken ct
    )
    {
        var rewardEvent = Deserialize<RewardRedeemedEvent>(json);

        if (rewardEvent is null)
        {
            logger.LogWarning(
                "Reward event on {RoutingKey} could not be deserialized, skipped",
                routingKey
            );
            return;
        }

        if (IsServiceActive && rewardEvent.Media is not null)
        {
            await SendAlertAsync(
                rewardEvent.Media,
                rewardEvent.User,
                rewardEvent.UserInput ?? string.Empty,
                ct
            );
        }
    }

    private async Task SendAlertAsync(
        MediaInfo media,
        TwitchUser? user,
        string message,
        CancellationToken ct
    )
    {
        var mediaClone = media.CloneTo();

        if (user is not null)
        {
            mediaClone.FixAlertText(user, message);
            mediaClone.FixAlertColor(user);
        }

        ct.ThrowIfCancellationRequested();

        await notifier.Alert(new MediaDto { MediaInfo = mediaClone });
    }
}
