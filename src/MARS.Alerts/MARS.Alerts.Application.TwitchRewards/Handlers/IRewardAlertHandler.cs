using System.Drawing;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Hosting;

namespace MARS.Alerts.Application.TwitchRewards.Handlers;

/// <summary>
/// Контракт обработчика награды для маршрутизации редемпшенов и команд.
/// </summary>
public interface IRewardAlertHandler : IHostedService
{
    int RewardCost { get; }
    string AlertDisplayName { get; set; }
    string AlertDescription { get; set; }
    Color Color { get; set; }
    Func<bool> IsRewardEnabled { get; set; }

    bool IsRewardActive { get; }
    bool IsRewardActivityWasConfermed { get; set; }

    /// <summary>
    /// Тип payload-а команды, которую обслуживает обработчик.
    /// <c>null</c> — обработчик не вызывается из команд.
    /// Ключ выводится из типа самого payload-а, без строкового хардкода.
    /// </summary>
    Type? CommandPayloadType { get; }

    Task<string> InvokeFromCommandAsync(
        AlertCommandPayload payload,
        CancellationToken cancellationToken = default
    );

    /// <summary>Прямой вызов обработчика (для тестов и совместимых consumer'ов).</summary>
    Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct);
}
