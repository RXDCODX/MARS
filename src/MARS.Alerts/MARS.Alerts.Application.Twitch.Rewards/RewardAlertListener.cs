using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Wolverine listener погашений наград. Фактическую маршрутизацию и передачу
/// события наследникам <see cref="RewardHandlerBase"/> выполняет общий менеджер.
/// </summary>
public sealed class RewardAlertListener(RewardEventManager eventManager)
    : MarsEventListener<RewardRedeemedEvent>
{
    public override Task Handle(RewardRedeemedEvent message, CancellationToken ct) =>
        eventManager.HandleRewardRedeemedAsync(message, ct);
}
