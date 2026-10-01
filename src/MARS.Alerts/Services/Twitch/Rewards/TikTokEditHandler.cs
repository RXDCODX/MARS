using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardTikTokEdit</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class TikTokEditHandler(ITelegramusNotifier notifier, RickRollerService rickRollerService)
    : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardTikTokEdit;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        var text = rewardEvent.UserInput ?? string.Empty;
        var user = rewardEvent.User;

        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                () => notifier.TikTokEdit(Guid.NewGuid(), text)
            );
        }
        else
        {
            await notifier.TikTokEdit(Guid.NewGuid(), text);
        }
    }
}
