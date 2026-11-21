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
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards;

/// <summary>
/// Обработчик reward-события <c>RewardCloseGame</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class CloseGameHandler(
    ILogger<CloseGameHandler> logger,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override string RewardCode => "close_game";
    public override int RewardCost { get; } = 6666;
    public override string AlertDisplayName { get; set; } = "💻 Выключить игру";
    public override string AlertDescription { get; set; } = "❌ Закрывает Tekken 8 или Dota 2";
    public override Color Color { get; set; } = Color.FromArgb(0, 128, 255);
    public override int Cost { get; init; } = 6666;
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
        var processNames = new[] { "Polaris-Win64-Shipping", "dota2" };
        foreach (var name in processNames)
        {
            var processes = System.Diagnostics.Process.GetProcessesByName(name);
            foreach (var process in processes)
            {
                try
                {
                    process.CloseMainWindow();
                    process.Kill();
                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error closing process {ProcessName}", name);
                }
            }
        }

        logger.LogInformation("CloseGame activated by {UserName}", rewardEvent.UserName);
    }
}
