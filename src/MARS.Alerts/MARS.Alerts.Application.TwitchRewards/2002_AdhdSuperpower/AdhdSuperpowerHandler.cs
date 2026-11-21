using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardAdhdSuperpower</c>.
/// </summary>
public class AdhdSuperpowerHandler(
    ILogger<AdhdSuperpowerHandler> logger,
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster
) : RewardHandlerBase
{
    public override string RewardCode => "adhd_superpower";

    public override string? CommandKey => "adhd";

    public override async Task<string> InvokeFromCommandAsync(
        AlertCommandPayload payload,
        CancellationToken cancellationToken = default
    )
    {
        if (payload is not AdhdAlertCommandPayload adhd)
        {
            return "Неверный тип параметров для adhd.";
        }

        var seconds = adhd.Seconds <= 0 ? 0 : adhd.Seconds;
        try
        {
            await _broadcaster.BroadcastAsync(
                new TelegramusEvent { Adhd = new AdhdEvent { Seconds = seconds } }
            );

            return seconds > 0
                ? $"✅ ADHD эффект активирован на {seconds} секунд!"
                : "✅ ADHD эффект переключён в перманентный режим!";
        }
        catch (Exception ex)
        {
            return $"❌ Ошибка при активации ADHD эффекта: {ex.Message}";
        }
    }

    private const int AdhdDurationSeconds = 60;

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
                        new TelegramusEvent
                        {
                            Adhd = new AdhdEvent { Seconds = AdhdDurationSeconds },
                        }
                    );
                }
            );
        }

        logger.LogInformation(
            "ADHD activated by {UserName} for {Duration} seconds",
            rewardEvent.UserName,
            AdhdDurationSeconds
        );
    }
}

