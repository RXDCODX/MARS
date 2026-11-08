using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using MARS.WaifuGacha.Configuration;
using MARS.WaifuGacha.Entities;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MARS.WaifuGacha.Services;

/// <summary>
/// Лисенер сообщений чата: при истечении кулдауна ролла для пользователя шлёт
/// уведомление в чат событием <see cref="ChatSendEvent"/>.
/// </summary>
public sealed class RollCooldownChatListener(
    RollCooldownNotificationState state,
    IMessageBus bus,
    ILogger<RollCooldownChatListener> logger
) : MarsEventListener<ChatEventMessageEvent>
{
    public override async Task Handle(ChatEventMessageEvent chatMessage, CancellationToken ct)
    {
        if (chatMessage is null || string.IsNullOrWhiteSpace(chatMessage.UserId))
        {
            return;
        }

        var expired = await state.GetExpiredAsync(chatMessage.UserId, ct);

        foreach (var (userId, rollType) in expired)
        {
            await NotifyAsync(userId, rollType, chatMessage.UserName, ct);
        }
    }

    private async Task NotifyAsync(
        string userId,
        string rollType,
        string userName,
        CancellationToken ct
    )
    {
        var alreadyNotified = await state.MarkNotifiedAsync(userId, rollType, ct);

        if (alreadyNotified)
        {
            return;
        }

        var rollName = RollTypeNames.GetValueOrDefault(rollType, rollType);
        var message = $"@{userName}, кулдаун на {rollName} прошел! Можешь использовать снова!";

        await bus.PublishAsync(
            new ChatSendEvent
            {
                Channel = TwitchConstants.Channel,
                Message = message,
                ReplyTo = userId,
            }
        );

        logger.LogInformation(
            "Отправлено уведомление о завершении кулдауна {RollType} для {UserName} ({UserId})",
            rollType,
            userName,
            userId
        );
    }

    private static readonly Dictionary<string, string> RollTypeNames = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        [RootStateKeys.WaifuRollType] = "WaifuRoll",
        [RootStateKeys.MikuRollType] = "MikuRoll",
        [RootStateKeys.FumoRollType] = "FumoRoll",
        [RootStateKeys.FrogRollType] = "FrogRoll",
    };
}
