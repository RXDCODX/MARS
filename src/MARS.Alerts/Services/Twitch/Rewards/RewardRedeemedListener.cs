using MARS.Shared.Messaging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Погашение награды: передаёт событие в <see cref="RewardAlertDispatcher"/>.
/// </summary>
/// <remarks>
/// Слушатель намеренно тонкий: правило «какая награда какой эффект показывает»
/// живёт в диспетчере, а тип-слушатель нужен Wolverine, чтобы завести отдельную
/// очередь. Имя класса обязано совпадать с записью в
/// <c>MarsQueues.QueueNameForHandlerType</c> — по нему выводится имя очереди.
/// </remarks>
public sealed class RewardRedeemedListener(RewardAlertDispatcher dispatcher)
{
    public Task Handle(RewardRedeemedEvent message, CancellationToken cancellationToken) =>
        dispatcher.DispatchAsync(message, cancellationToken);
}
