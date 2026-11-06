using MARS.Alerts.Services.Alerts;
using MARS.Alerts.Services.RewardInput;
using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Platform.Web.AspNetCore.Infrastructure.Configuration;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services.RewardInput;

/// <summary>
/// Потребитель сообщений, отправленных через награду: показывает алерт,
/// привязанный к этой награде.
/// </summary>
/// <remarks>
/// Очередь <c>alerts.rewardinput</c> с биндингом ровно на
/// <see cref="RabbitMqConfig.RewardInputMessage"/>.
/// </remarks>
public class RewardInputMessageConsumer(
    IOptions<RabbitMqOptions> options,
    RewardBoundAlertDispatcher dispatcher,
    ILogger<RewardInputMessageConsumer> logger
)
    : RabbitMqConsumerBase(
        options.Value,
        "alerts",
        QueueName,
        [RabbitMqConfig.RewardInputMessage],
        logger
    )
{
    public const string QueueName = "alerts.rewardinput";

    protected override async Task HandleMessageAsync(
        string routingKey,
        string json,
        CancellationToken ct
    )
    {
        var chatMessage = Deserialize<ChatEventMessageEvent>(json);

        if (chatMessage is null)
        {
            return;
        }

        await dispatcher.DispatchRewardInputAsync(chatMessage, ToUser(chatMessage), ct);
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
