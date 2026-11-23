using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Kernel;

namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Событие погашения награды канала. Единственное reward-событие шины:
/// конкретная награда определяется по <see cref="RewardTitle"/>, а не по
/// отдельному exchange/routing key.
/// </summary>
/// <remarks>
/// Направление: <c>ИЗ MARS.Twitch => В MARS.Alerts</c>. Публикуется
/// <c>RewardRedemptionPublisher</c> (реальные погашения) и <c>HelloVideoNotifier</c>
/// (синтетическое hello-video, <c>RewardId = Guid.Empty</c>); потребляют
/// <c>RewardAlertListener</c>, <c>TwitchMediaAlertsListener</c>,
/// <c>CostMatchedRewardListener</c>, <c>AllRefundRedemptionListener</c>.
/// Поле <c>Media</c> заполняется <c>IMediaStorageClient.GetMediaInfoAsync</c>
/// в <c>RewardRedemptionPublisher</c>, а не через <c>ITwitchMediaPreparationService</c>.
/// </remarks>
public sealed class RewardRedeemedEvent : IMarsEventMessage
{
    public string RewardId { get; set; } = string.Empty;
    public string RewardCode { get; set; } = string.Empty;
    public string RewardTitle { get; set; } = string.Empty;
    public int Cost { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string BroadcasterUserId { get; set; } = string.Empty;
    public bool IsFollower { get; set; }
    public string? UserInput { get; set; }
    public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;
    public string? MessageId { get; set; }
    public TwitchUser? User { get; set; }

    /// <summary>
    /// Медиа-контент награды, если награда привязана к файлу.
    /// Заполняет публикующая сторона (MARS.TwitchCore) через
    /// <c>ITwitchMediaPreparationService</c>. Потребитель MARS.Alerts
    /// (<c>TwitchMediaAlerts</c>) читает именно это поле.
    /// </summary>
    public MediaInfo? Media { get; set; }
}
