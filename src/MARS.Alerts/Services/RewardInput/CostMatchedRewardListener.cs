using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Telemetry;

namespace MARS.Alerts.Services.RewardInput;

/// <summary>
/// Погашение награды без текста пользователя: показывает алерт с той же ценой в
/// баллах канала.
/// </summary>
/// <remarks>
/// Отбор по цене пропускается, когда событие уже несёт медиа: такой алерт
/// показывает <c>TwitchMediaListener</c>, и второй показ был бы дублем.
/// </remarks>
public sealed class CostMatchedRewardListener(RewardBoundAlertDispatcher dispatcher)
{
    public Task Handle(RewardRedeemedEvent message, CancellationToken cancellationToken)
    {
        var user =
            message.User
            ?? new TwitchUser
            {
                TwitchId = message.UserId,
                UserLogin = message.UserName,
                DisplayName = message.UserName,
            };

        return dispatcher.DispatchRedemptionAsync(message, user, cancellationToken);
    }
}
