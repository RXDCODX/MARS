using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Сообщение, отправленное через награду с вводом текста. Отдельный контракт,
/// потому что в оверлей чата такое сообщение не попадает, но по нему
/// поднимается привязанный к награде алерт (AC.S08). Потребители обычного чата
/// не должны видеть такие сообщения.
/// </summary>
public sealed class RewardInputChatMessageEvent : IMarsEventMessage
{
    public ChatEventMessageEvent Message { get; set; } = new();
}
