using System.Text.Json;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchMessages;

/// <summary>
/// Ретранслирует обычное сообщение чата Twitch в оверлейный хаб.
/// </summary>
/// <remarks>
/// <para>
/// В монолите сообщение уходило в оверлей напрямую (<c>notifier.NewMessage</c>),
/// потому что хаб жил в том же процессе. В микросервисах хаб живёт в
/// <c>MARS.Alerts</c>, а <c>MARS.TwitchCore</c> публикует <see cref="ChatEventMessageEvent"/>
/// в RabbitMQ — этот листенер его подхватывает и кладёт в broadcaster, откуда
/// <c>HubEventRelay</c> рассылает его в браузер как <c>NewMessage</c>.
/// </para>
/// <para>
/// Наградные сообщения (<c>CustomRewardId</c>) сюда не приходят: продюсер
/// (<c>TwitchMessagesPublisher</c>) отделяет их в <c>RewardInputChatMessageEvent</c>,
/// как и в монолите.
/// </para>
/// </remarks>
public sealed class OverlayChatMessageListener(
    ILogger<OverlayChatMessageListener> logger,
    IHubContext<AlertsHub, IAlertsHub> hubContext
) : MarsEventListener<ChatEventMessageEvent>
{
    private readonly IHubContext<AlertsHub, IAlertsHub> _hubContext = hubContext;

    public override async Task Handle(ChatEventMessageEvent message, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        logger.LogDebug("Chat message received from {UserName}", message.UserName);

        // Сообщение уходит как объект: фронтенд читает те же поля
        // (userName, chatColor, isModerator, isVip, message) напрямую,
        // без JSON.parse внутри MessageJson.
        await _hubContext.Clients.All.NewMessage(
            new NewMessageEvent { Id = message.Id, Message = message }
        );
    }
}
