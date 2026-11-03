using MARS.Shared.Messaging;
using MARS.Shared.Models.Media;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Находит файл награды: сначала в самом событии, затем в хранилище.
/// </summary>
public interface IRewardMediaResolver
{
    /// <summary>
    /// Возвращает медиа награды либо пустой список, если файла нет. Пустой
    /// список — это норма: у большинства наград файла нет вовсе, и рисовать
    /// им нечего.
    /// </summary>
    Task<IReadOnlyList<MediaInfo>> ResolveAsync(
        RewardRedeemedEvent reward,
        CancellationToken cancellationToken = default
    );
}
