namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Запрос на возврат баллов канальной награды (статус CANCELED).
/// Публикуют сервисы, у которых нет собственного Helix-подключения;
/// обрабатывает MARS.Twitch.Api — владелец единственного подключения к Twitch.
/// </summary>
public sealed class RewardRefundEvent : IMarsEventMessage
{
    /// <summary>Идентификатор награды Twitch (reward id).</summary>
    public string RewardId { get; init; } = string.Empty;

    /// <summary>Идентификаторы редемпшенов, которые нужно отменить.</summary>
    public List<string> RedemptionIds { get; init; } = [];

    /// <summary>Причина возврата (для логов).</summary>
    public string Reason { get; init; } = string.Empty;
}
