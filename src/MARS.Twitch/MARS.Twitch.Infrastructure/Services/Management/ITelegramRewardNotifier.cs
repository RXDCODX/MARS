namespace MARS.Twitch.Infrastructure.Services.Management;

/// <summary>
/// Уведомление администраторов о неоднозначностях и ошибках синхронизации наград.
/// </summary>
public interface ITelegramRewardNotifier
{
    Task NotifyRewardSyncIssueAsync(string message, CancellationToken cancellationToken = default);
}
