using MARS.Alerts.Services.RewardInput;
using MARS.Shared.Messaging;
using MARS.Shared.Models;

namespace MARS.Alerts.Services.RewardInput;

/// <summary>
/// Сообщение, отправленное через награду: показывает привязанный к награде алерт.
/// </summary>
public sealed class RewardInputMessageListener(RewardBoundAlertDispatcher dispatcher)
{
    public Task Handle(RewardInputMessageEvent message, CancellationToken cancellationToken) =>
        dispatcher.DispatchRewardInputAsync(message, ToUser(message), cancellationToken);

    private static TwitchUser ToUser(ChatMessageEvent chatMessage) =>
        new()
        {
            TwitchId = chatMessage.UserId,
            UserLogin = chatMessage.UserName,
            DisplayName = chatMessage.UserName,
            ChatColor = chatMessage.ChatColor,
            IsModerator = chatMessage.IsModerator,
            IsVip = chatMessage.IsVip,
        };
}
