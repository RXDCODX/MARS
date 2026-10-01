using MARS.Alerts.Models;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardFireworks</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class FireworksHandler(ITelegramusNotifier notifier, RickRollerService rickRollerService)
    : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardFireworks;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        var user = rewardEvent.User;
        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                () => notifier.MakeScreenParticles(TwitchScreenParticles.Fireworks)
            );
        }
        else
        {
            await notifier.MakeScreenParticles(TwitchScreenParticles.Fireworks);
        }
    }
}
