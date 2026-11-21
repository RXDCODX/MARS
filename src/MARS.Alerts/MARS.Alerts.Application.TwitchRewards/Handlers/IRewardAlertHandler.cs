using System.Drawing;
using Microsoft.Extensions.Hosting;

namespace MARS.Alerts.Application.TwitchRewards.Handlers;

/// <summary>
/// Обработчик одной награды. Все обработчики разбирает единственный
/// <see cref="RewardAlertConsumer"/>: отдельного AMQP-соединения на награду нет.
/// </summary>
public interface IRewardAlertHandler : IHostedService
{
    /// <summary>
    /// Локальный ключ награды без префикса и внешнего exchange:
    /// нормализованное название, например <c>phonkedit</c>.
    /// </summary>
    int RewardCost { get; }
    public string AlertDisplayName { get; set; }
    public string AlertDescription { get; set; }
    public Color Color { get; set; }
    public Func<bool> IsRewardEnabled { get; set; }
    protected bool IsRewardActive { get; }
    bool IsRewardActivityWasConfermed { get; set; }
}
