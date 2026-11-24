using MARS.Alerts.Application.TwitchRewards.Handlers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards.Validation;

public sealed class RewardConflictValidationService(
    IEnumerable<IRewardAlertHandler> handlers,
    ILogger<RewardConflictValidationService> logger
) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            RewardConflictValidator.ThrowIfIssues(handlers);
            logger.LogInformation(
                "Проверка конфликтов наград пройдена ({Count} хендлеров)",
                handlers.Count()
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Обнаружены конфликты наград");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
