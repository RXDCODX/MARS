using System.Threading;
using System.Threading.Tasks;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Shared.Contracts.Twitch.Rest;

/// <summary>
/// Внутренний клиент MARS.Twitch для межсервисных операций (чат, VIP, возврат
/// баллов, результаты мини-игр). Владелец — MARS.Twitch.Api (Internal-эндпоинты
/// с политикой ServiceApiKey). Заменяет публикацию ChatSendEvent/ChatAnnouncementEvent/
/// TwitchVipGrantEvent/RewardRefundEvent/MiniGameResultEvent через брокер.
/// </summary>
public interface ITwitchInternalClient
{
    /// <summary>Отправить сообщение в чат Twitch (включая анонсы — Helix-анонсы недоступны в этой версии TwitchLib).</summary>
    Task<bool> SendChatMessageAsync(
        TwitchChatSendRequest request,
        CancellationToken cancellationToken = default
    );

    Task<bool> GrantVipAsync(
        TwitchVipGrantRequest request,
        CancellationToken cancellationToken = default
    );

    Task<bool> RefundRewardAsync(
        RewardRefundRequest request,
        CancellationToken cancellationToken = default
    );

    Task<bool> RecordMiniGameResultAsync(
        MiniGameResultRequest request,
        CancellationToken cancellationToken = default
    );
}
