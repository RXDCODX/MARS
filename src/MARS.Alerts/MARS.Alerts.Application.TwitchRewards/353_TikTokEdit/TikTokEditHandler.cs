using System.Drawing;
using MARS.Alerts.Application.Overlay;
using MARS.Alerts.Application.Services;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Hosting;

namespace MARS.Alerts.Application.TwitchRewards._353_TikTokEdit;

/// <summary>
/// Обработчик reward-события <c>RewardTikTokEdit</c>.
/// </summary>
public class TikTokEditHandler(
    RickRollerService rickRollerService,
    IOverlayEventSink broadcaster,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override string RewardCode => "tiktok_edit";
    public override int RewardCost { get; } = 353;
    public override string AlertDisplayName { get; set; } = "🎬 Make a TikTok Edit";
    public override string AlertDescription { get; set; } =
        "📝 текст наверху и внизу можно разделить символом `=`";
    public override Color Color { get; set; } = Color.FromArgb(245, 0, 0);
    public override int Cost { get; init; } = 353;
    public override Func<bool> IsRewardEnabled { get; set; } = () => true;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        SubscribeOnStart(
            lifetime,
            rewardEventManager,
            onRedeem: ValidatedRedeem(validator, OnRewardRedeemedAsync, Cost, IsRewardEnabled)
        );
        return Task.CompletedTask;
    }

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
                    await broadcaster.BroadcastAsync(
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
