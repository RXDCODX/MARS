using System.Text.Json.Nodes;
using MARS.Shared.Messaging;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Обрабатывает Channel Points notification из общего EventSub JSON-потока.
/// </summary>
public interface IRewardRedemptionProcessor
{
    /// <summary>
    /// Добавляет к исходному EventSub JSON необязательное MARS-обогащение.
    /// Возвращает null, если специальное поведение намеренно подавило показ
    /// (например, сработал рикролл или эффекту нечего отображать).
    /// </summary>
    Task<string?> ProcessAsync(
        string rawEventJson,
        string? alertKey,
        Guid? mediaInfoId,
        CancellationToken cancellationToken
    );
}
