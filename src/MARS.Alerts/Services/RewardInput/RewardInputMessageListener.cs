using MARS.Alerts.Services.Alerts;
using MARS.Alerts.Services.RewardInput;
using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;

namespace MARS.Alerts.Services.RewardInput;

/// <summary>
/// Потребитель сообщений, отправленных через награду: показывает алерт,
/// привязанный к этой награде.
/// </summary>
public sealed class RewardInputMessageListener(RewardBoundAlertDispatcher dispatcher)
    : MarsEventListener<RewardInputChatMessageEvent>
{
    public override async Task Handle(RewardInputChatMessageEvent message, CancellationToken ct)
    {
        if (message?.Message is null)
        {
            return;
        }

        await dispatcher.DispatchRewardInputAsync(message.Message, ToUser(message.Message), ct);
    }

    private static TwitchUser ToUser(ChatEventMessageEvent chatEventMessage) =>
        new()
        {
            TwitchId = chatEventMessage.UserId,
            UserLogin = chatEventMessage.UserName,
            DisplayName = chatEventMessage.UserName,
            ChatColor = chatEventMessage.ChatColor,
            IsModerator = chatEventMessage.IsModerator,
            IsVip = chatEventMessage.IsVip,
        };
}
