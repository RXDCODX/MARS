using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardCloseGame</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class CloseGameHandler(ILogger<CloseGameHandler> logger) : RewardHandlerBase
{
    public string RewardKey => "closegame";

    protected override async Task OnRewardRedeemedAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
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
