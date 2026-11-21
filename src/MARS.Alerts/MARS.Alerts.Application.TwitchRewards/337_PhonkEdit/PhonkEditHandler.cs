using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;

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

    private readonly EventBroadcaster<TelegramusEvent> _broadcaster = broadcaster;

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
                    await _broadcaster.BroadcastAsync(
                        new TelegramusEvent { PhonkEdit = new EmptyEvent() }
                    );
                }
            );
        }
    }
}
