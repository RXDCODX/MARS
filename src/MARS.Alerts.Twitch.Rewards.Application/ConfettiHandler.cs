using MARS.Alerts.Twitch.Rewards.Application.Models;
using MARS.Platform.Infrastructure.Grpc.Notifications;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Twitch.Rewards.Application;

/// <summary>
/// Обработчик reward-события <c>RewardConfetti</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class ConfettiHandler(ITelegramusNotifier notifier, RickRollerService rickRollerService)
    : IRewardAlertHandler
{
    public string RewardKey => "confetti";

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        var user = rewardEvent.User;
        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                () => notifier.MakeScreenParticles(TwitchScreenParticles.Confetty)
            );
        }
        else
        {
            await notifier.MakeScreenParticles(TwitchScreenParticles.Confetty);
        }
    }
}
