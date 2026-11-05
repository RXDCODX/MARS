using MARS.Shared.Messaging;
using MARS.WaifuGacha.Services;
using Wolverine;

namespace MARS.WaifuGacha.Services;

/// <summary>
/// Слушатель погашений наград: проверяет, не пора ли предупредить зрителей,
/// что кулдаун на ролл прошёл.
/// </summary>
public sealed class RollCooldownListener(RollCooldownNotificationService service)
{
    public Task Handle(RewardRedeemedEvent message, CancellationToken cancellationToken) =>
        service.OnRewardRedeemedAsync(message, cancellationToken);
}
