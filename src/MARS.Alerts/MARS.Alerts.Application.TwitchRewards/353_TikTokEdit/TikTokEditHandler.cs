using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;

namespace MARS.Alerts.Application.TwitchRewards._353_TikTokEdit;

/// <summary>
/// Обработчик reward-события <c>RewardTikTokEdit</c>.
/// </summary>
public class TikTokEditHandler(
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster
) : RewardHandlerBase
{
    public override string RewardCode => "tiktok_edit";

    private readonly EventBroadcaster<TelegramusEvent> _broadcaster = broadcaster;

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var text = rewardEvent.UserInput ?? string.Empty;

        var user = rewardEvent.User;
        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                async () =>
                {
                    await _broadcaster.BroadcastAsync(
                        new TelegramusEvent
                        {
                            TikTokEdit = new TikTokEditEvent
                            {
                                Guid = Guid.NewGuid().ToString(),
                                Text = text,
                            },
                        }
                    );
                }
            );
        }
    }
}
