using MARS.Shared.Messaging;
using MARS.Shared.Models;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

public sealed class RewardChatNotifier(IMarsEventBus eventBus) : IRewardChatNotifier
{
    public Task SendAsync(
        string message,
        string? replyTo = null,
        CancellationToken cancellationToken = default
    )
    {
        return eventBus.PublishAsync(
            RabbitMqConfig.RoutingKeys.ChatSend,
            new ChatSendEvent
            {
                Channel = TwitchChatChannel.Name,
                Message = message,
                ReplyTo = replyTo,
            },
            cancellationToken
        );
    }
}
