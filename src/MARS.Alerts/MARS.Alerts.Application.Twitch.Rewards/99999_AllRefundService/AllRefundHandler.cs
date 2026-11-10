using MARS.Platform.Infrastructure.Grpc.Notifications;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardAllRefund</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class AllRefundHandler(ITelegramusNotifier notifier, ILogger<AllRefundHandler> logger)
    : RewardHandlerBase
{
    public override string RewardKey => "allrefund";

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var user = rewardEvent.User;
        if (user is not null)
        {
            await notifier.AllRefund(user);
        }

        logger.LogInformation("AllRefund activated by {UserName}", rewardEvent.UserName);
    }
}
