using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards;

public interface IRewardActivationPolicy
{
    ValueTask<bool> IsActiveAsync(RewardRedeemedEvent reward, CancellationToken cancellationToken);

    /// <summary>
    /// Участвует ли политика в планировании активности награды на канале
    /// (переключение IsEnabled на стороне Twitch). Stateful-политики
    /// (кулдауны) не участвуют: их проверяет только гейт диспатча.
    /// </summary>
    bool IsStateful => false;

    /// <summary>
    /// Планировочная активность (без учёта кулдаунов): используется
    /// сервисом синхронизации MARS.Twitch для переключения IsEnabled.
    /// </summary>
    ValueTask<bool> IsActiveForSchedulingAsync(CancellationToken cancellationToken) =>
        IsStateful
            ? ValueTask.FromResult(true)
            : IsActiveAsync(new RewardRedeemedEvent(), cancellationToken);
}
