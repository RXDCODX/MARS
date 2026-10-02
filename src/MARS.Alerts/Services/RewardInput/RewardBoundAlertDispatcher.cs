using MARS.Alerts.Extensions;
using MARS.Alerts.Services.Alerts;
using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;

namespace MARS.Alerts.Services.RewardInput;

/// <summary>
/// Показ алерта, привязанного к награде. Перенос
/// <c>TwitchEventSubAlertsAwaker</c> монолита: тот вёл два отбора по таблице
/// алертов, и оба потерялись при переносе.
/// </summary>
/// <remarks>
/// <list type="number">
/// <item>сообщение в чате с непустым <c>CustomRewardId</c> — отбор по
/// <c>MetaInfo.TwitchGuid</c>: это «тот же алерт от обычного сообщения», путь
/// <c>ClientKeyTriggerAlert</c> из <c>RewardRedemptionPublisher</c> был удалён
/// вместе с монолитным издателем и не восстанавливался (блокер №7);</item>
/// <item>погашение награды без текста пользователя — отбор по
/// <c>MetaInfo.TwitchPointsCost</c>. Монолит не смотрел на привязку награды, и
/// алерт без <c>ChannelRewardRecord</c> поднимался по одной только цене.</item>
/// </list>
/// Из нескольких подходящих показывается один случайный — так же, как в
/// монолите. Отбор по цене пропускается, если событие уже несёт
/// <see cref="RewardRedeemedEvent.Media"/>: этот алерт показывает
/// <c>TwitchMediaAlerts</c>, и второй показ того же файла был бы дублем.
/// </remarks>
public sealed class RewardBoundAlertDispatcher(
    IEnabledAlertSource alertSource,
    ITelegramusNotifier notifier,
    RickRollerService rickRoller,
    ILogger<RewardBoundAlertDispatcher> logger
)
{
    public async Task DispatchRewardInputAsync(
        ChatMessageEvent chatMessage,
        TwitchUser user,
        CancellationToken cancellationToken
    )
    {
        if (!Guid.TryParse(chatMessage.CustomRewardId, out var rewardGuid))
        {
            return;
        }

        var alerts = await GetAlertsAsync(rewardGuid, null, cancellationToken);

        if (alerts.Count > 0)
        {
            await ShowAsync(alerts, user, chatMessage.Message, false);
        }
    }

    public async Task DispatchRedemptionAsync(
        RewardRedeemedEvent rewardEvent,
        TwitchUser user,
        CancellationToken cancellationToken
    )
    {
        if (!string.IsNullOrWhiteSpace(rewardEvent.UserInput) || rewardEvent.Media is not null)
        {
            return;
        }

        var alerts = await GetAlertsAsync(null, rewardEvent.Cost, cancellationToken);

        if (alerts.Count > 0)
        {
            await ShowAsync(alerts, user, string.Empty, true);
        }
    }

    private async Task<List<MediaInfo>> GetAlertsAsync(
        Guid? rewardGuid,
        int? pointsCost,
        CancellationToken cancellationToken
    )
    {
        var result = new List<MediaInfo>();
        var alerts = await alertSource.GetEnabledAlertsAsync(cancellationToken);

        if (alerts is null)
        {
            return result;
        }

        foreach (var alert in alerts)
        {
            // Цена 0 — это «цена не задана»: у бесплатных наград она совпадает
            // со значением по умолчанию, и сравнивать их нельзя.
            var matchesReward = rewardGuid is { } guid && alert.MetaInfo.TwitchGuid == guid;
            var matchesCost =
                pointsCost is { } cost && cost > 0 && alert.MetaInfo.TwitchPointsCost == cost;

            if (matchesReward || matchesCost)
            {
                result.Add(alert);
            }
        }

        return result;
    }

    private async Task ShowAsync(
        List<MediaInfo> alerts,
        TwitchUser user,
        string message,
        bool withRickRoll
    )
    {
        var shuffled = alerts.ToArray();
        Random.Shared.Shuffle(shuffled);

        var source = shuffled[0];

        async Task ShowAsync()
        {
            var alert = source.CloneTo();
            alert.FixAlertText(user, message);
            alert.FixAlertColor(user);

            await notifier.Alert(new MediaDto(alert) { MediaInfo = alert });

            logger.LogInformation(
                "Алерт {AlertId} показан по награде от {UserName}",
                alert.Id,
                user.DisplayName
            );
        }

        if (withRickRoll)
        {
            await rickRoller.TryRickRollAsync(user, ShowAsync);
        }
        else
        {
            await ShowAsync();
        }
    }
}
