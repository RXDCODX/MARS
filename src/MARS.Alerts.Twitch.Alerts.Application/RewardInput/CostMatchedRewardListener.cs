using MARS.Alerts.Services.RewardInput;
using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;

namespace MARS.Alerts.Twitch.Alerts.Application.RewardInput;

/// <summary>
/// Потребитель погашений наград без текста пользователя: показывает алерт,
/// у которого заявлена та же цена в баллах канала.
/// </summary>
public sealed class CostMatchedRewardListener(RewardBoundAlertDispatcher dispatcher)
    : MarsEventListener<RewardRedeemedEvent>
{
    public override async Task Handle(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        if (rewardEvent is null)
        {
            return;
        }

        var user =
            rewardEvent.User
            ?? new TwitchUser
            {
                TwitchId = rewardEvent.UserId,
                UserLogin = rewardEvent.UserName,
                DisplayName = rewardEvent.UserName,
            };

        await dispatcher.DispatchRedemptionAsync(rewardEvent, user, ct);
    }
}
