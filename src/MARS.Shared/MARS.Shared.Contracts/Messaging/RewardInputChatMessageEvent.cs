using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Kernel;

namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Сообщение, отправленное через награду с вводом текста. Отдельный контракт,
/// потому что в оверлей чата такое сообщение не попадает, но по нему
/// поднимается привязанный к награде алерт (AC.S08). Потребители обычного чата
/// не должны видеть такие сообщения.
/// </summary>
/// <remarks>
/// Направление: <c>ИЗ MARS.Twitch => В MARS.Alerts</c>. Публикуется
/// <c>TwitchMessagesPublisher</c> (при непустом <c>CustomRewardId</c>);
/// потребляют <c>RewardInputMessageListener</c>, <c>CostMatchedRewardListener</c>,
/// <c>TriggerWordRewardInputListener</c>, <c>TwitchRewardInputListener</c>,
/// <c>TwitchRewardInputMessageListener</c> (MARS.Alerts).
/// </remarks>
public sealed class RewardInputChatMessageEvent : IMarsEventMessage
{
    public ChatEventMessageEvent Message { get; set; } = new();
}
