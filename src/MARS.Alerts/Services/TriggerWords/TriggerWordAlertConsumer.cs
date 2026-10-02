using MARS.Shared.Clients;
using MARS.Shared.Configuration;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Media;
using MARS.Shared.Messaging;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services.TriggerWords;

/// <summary>
/// Показ алерта, когда зритель написал в чат ключевое слово.
/// </summary>
/// <remarks>
/// Перенесено из <c>TwitchMessagesHubAwaker</c> монолита. Там подбор алертов
/// шёл прямо на стороне MARS.TwitchCore, где таблицы алертов не было: список
/// приезжал через общий <c>AppDbContext</c>. Здесь их читает владелец —
/// MARS.MediaStorage.
/// </remarks>
public sealed class TriggerWordAlertConsumer(
    IOptions<RabbitMqOptions> options,
    ITriggerWordAlertSource alertSource,
    ITelegramusNotifier notifier,
    ILogger<TriggerWordAlertConsumer> logger
)
    : RabbitMqConsumerBase(
        options.Value,
        "alerts",
        QueueName,
        [RabbitMqConfig.MessageReceived],
        logger
    )
{
    public const string QueueName = "alerts.triggerwords";

    protected override async Task HandleMessageAsync(
        string routingKey,
        string json,
        CancellationToken ct
    )
    {
        var chatMessage = Deserialize<ChatMessageEvent>(json);

        if (chatMessage is null || string.IsNullOrWhiteSpace(chatMessage.Message))
        {
            return;
        }

        var alerts = await alertSource.GetEnabledAlertsAsync(ct);

        if (alerts is null || alerts.Count == 0)
        {
            return;
        }

        foreach (var alert in TriggerWordMatcher.Match(alerts, chatMessage.Message))
        {
            var dto = alert.CloneTo();
            dto.TextInfo.Text = chatMessage.Message;
            dto.MetaInfo.DisplayName = chatMessage.UserName ?? chatMessage.UserId;

            await notifier.Alert(new MediaDto(dto) { MediaInfo = dto });

            logger.LogInformation(
                "Алерт {AlertId} показан по ключевому слову сообщения от {UserName}",
                dto.Id,
                chatMessage.UserName
            );
        }
    }
}
