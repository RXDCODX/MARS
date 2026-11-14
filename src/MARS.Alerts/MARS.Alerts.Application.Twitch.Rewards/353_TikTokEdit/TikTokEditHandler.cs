using MARS.Alerts.Application.Twitch.Rewards.Handlers;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.Twitch.Rewards._353_TikTokEdit;

/// <summary>
/// Обработчик reward-события <c>RewardTikTokEdit</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class TikTokEditHandler(RickRollerService rickRollerService) : RewardHandlerBase
{
    public override string RewardKey => "tiktokedit";

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var text = rewardEvent.UserInput ?? string.Empty;
        var user = rewardEvent.User;

        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(user, () => Task.CompletedTask);
        }
        else
        {
            //await notifier.TikTokEdit(Guid.NewGuid(), text);
        }
    }
}
