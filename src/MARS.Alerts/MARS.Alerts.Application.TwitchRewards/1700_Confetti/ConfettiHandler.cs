using System.Text.Json;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;

namespace MARS.Alerts.Application.Twitch.Rewards._1700_Confetti;

/// <summary>
/// Обработчик reward-события <c>RewardConfetti</c>.
/// </summary>
public class ConfettiHandler(
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster
) : RewardHandlerBase
{
<<<<<<< HEAD:src/MARS.Alerts/MARS.Alerts.Application.TwitchRewards/1700_Confetti/ConfettiHandler.cs
    public override string RewardCode => "confetti";
=======
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
>>>>>>> origin/main:src/MARS.Alerts/MARS.Alerts.Application.Twitch.Rewards/2002_AdhdSuperpower/AdhdSuperpowerHandler.cs

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
                            MakeScreenParticles = new ScreenParticlesEvent
                            {
                                ParticlesJson = JsonSerializer.Serialize("Confetty"),
                            },
                        }
                    );
                }
            );
        }
    }
}

