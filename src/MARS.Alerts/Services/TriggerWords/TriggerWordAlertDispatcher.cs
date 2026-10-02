using MARS.Alerts.Extensions;
using MARS.Alerts.Services.Alerts;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Media;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;

namespace MARS.Alerts.Services.TriggerWords;

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
    ITelegramusNotifier notifier,
    ILogger<TriggerWordAlertDispatcher> logger
)
{
    public async Task DispatchAsync(
        ChatMessageEvent chatMessage,
        CancellationToken cancellationToken
    )
    {
        var alerts = await alertSource.GetEnabledAlertsAsync(cancellationToken);

        if (alerts is null || alerts.Count == 0)
        {
            return;
        }

        var matched = TriggerWordMatcher.Match(alerts, chatMessage.Message).ToList();

        if (matched.Count == 0)
        {
            return;
        }

        var shuffled = matched.ToArray();
        Random.Shared.Shuffle(shuffled);

        var user = new TwitchUser
        {
            TwitchId = chatMessage.UserId,
            UserLogin = chatMessage.UserName,
            DisplayName = chatMessage.UserName,
            ChatColor = chatMessage.ChatColor,
            IsModerator = chatMessage.IsModerator,
            IsVip = chatMessage.IsVip,
        };

        var alert = shuffled[0].CloneTo();
        alert.FixAlertText(user, chatMessage.Message);
        alert.FixAlertColor(user);

        await notifier.Alert(new MediaDto(alert) { MediaInfo = alert });

        logger.LogInformation(
            "Алерт {AlertId} показан по ключевому слову сообщения от {UserName}",
            alert.Id,
            user.DisplayName
        );
    }
}
