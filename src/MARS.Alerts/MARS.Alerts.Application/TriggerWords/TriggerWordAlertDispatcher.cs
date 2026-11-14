using MARS.Alerts.Application.Alerts;
using MARS.Alerts.Application.Twitch.Rewards.Extensions;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TriggerWords;

/// <summary>
/// Показ алерта, когда зритель написал в чат ключевое слово.
/// </summary>
/// <remarks>
/// Перенесено из <c>TwitchMessagesHubAwaker.ClientKeyTriggerAlert</c> монолита.
/// Там подбор алертов шёл на стороне MARS.TwitchCore, где таблицы алертов нет:
/// список приезжал через общий <c>AppDbContext</c>. Здесь алерты читает владелец —
/// MARS.MediaStorage.
/// <para>
/// Из подходящих показывается один случайный (в монолите — shuffle и первый
/// элемент): сообщение в чате не должно превращать оверлей в стену одинаковых
/// картинок. Шаблон алерта при этом сохраняется — <c>{user.text}</c>,
/// <c>{user.name}</c> и <c>{user.color}</c> подставляются от автора сообщения.
/// </para>
/// </remarks>
public sealed class TriggerWordAlertDispatcher(
    IEnabledAlertSource alertSource,

    ILogger<TriggerWordAlertDispatcher> logger
)
{
    public async Task DispatchAsync(
        ChatEventMessageEvent chatEventMessage,
        CancellationToken cancellationToken
    )
    {
        var alerts = await alertSource.GetEnabledAlertsAsync(cancellationToken);

        if (alerts is null || alerts.Count == 0)
        {
            return;
        }

        var matched = TriggerWordMatcher.Match(alerts, chatEventMessage.Message).ToList();

        if (matched.Count == 0)
        {
            return;
        }

        var shuffled = matched.ToArray();
        Random.Shared.Shuffle(shuffled);

        var user = new TwitchUser
        {
            TwitchId = chatEventMessage.UserId,
            UserLogin = chatEventMessage.UserName,
            DisplayName = chatEventMessage.UserName,
            ChatColor = chatEventMessage.ChatColor,
            IsModerator = chatEventMessage.IsModerator,
            IsVip = chatEventMessage.IsVip,
        };

        var alert = shuffled[0].CloneTo();
        alert.FixAlertText(user, chatEventMessage.Message);
        alert.FixAlertColor(user);

        //await notifier.Al7ert(new MediaDto(alert) { MediaInfo = alert });

        logger.LogInformation(
            "Алерт {AlertId} показан по ключевому слову сообщения от {UserName}",
            alert.Id,
            user.DisplayName
        );
    }
}
