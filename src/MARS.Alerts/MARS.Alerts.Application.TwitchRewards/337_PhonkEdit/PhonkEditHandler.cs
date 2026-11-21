using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.Services;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards;

/// <summary>
/// Обработчик reward-события <c>RewardPhonkEdit</c>.
/// </summary>
public class PhonkEditHandler(
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster
) : RewardHandlerBase
{
    public override string RewardCode => "phonk_edit";

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var user = rewardEvent.User;
        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                async () =>
                {
                    await broadcaster.BroadcastAsync(
                        new TelegramusEvent { PhonkEdit = new EmptyEvent() }
                    );
                }
            );
        }
    }
}
