using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardCloseGame</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class CloseGameHandler(ILogger<CloseGameHandler> logger) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardCloseGame;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
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
