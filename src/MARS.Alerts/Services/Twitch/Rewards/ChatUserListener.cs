using MARS.Shared.Messaging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Поток сообщений чата — единственный источник данных для
/// <see cref="IChatUserTrackingHandler"/>.
/// </summary>
/// <remarks>
/// Раньше поток сообщений чата не читался ни одним сервисом вообще, поэтому
/// MIKU MIKU BEAM всегда показывал пустой набор участников.
/// </remarks>
public sealed class ChatUserListener(IEnumerable<IChatUserTrackingHandler> handlers)
{
    private readonly IReadOnlyList<IChatUserTrackingHandler> _handlers = [.. handlers];

    public Task Handle(ChatMessageReceivedEvent message, CancellationToken cancellationToken)
    {
        if (_handlers.Count > 0 && !string.IsNullOrWhiteSpace(message.UserId))
        {
            foreach (var handler in _handlers)
            {
                handler.TrackChatUser(message);
            }
        }

        return Task.CompletedTask;
    }
}
