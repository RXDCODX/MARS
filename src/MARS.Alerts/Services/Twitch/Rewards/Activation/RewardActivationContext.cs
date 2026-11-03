using MARS.Shared.Messaging;
using MARS.Shared.Models.Media;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Всё, что известно об активации награды к моменту вызова эффекта.
/// </summary>
/// <remarks>
/// <para>
/// Контекст намеренно не содержит <see cref="ITelegramusNotifier"/> и не умеет
/// рисовать: эффект возвращает данные, а отправляет их один
/// <see cref="RewardActivatedConsumer"/>. Иначе каждый эффект рисовал бы
/// по-своему, и клиент снова получал бы события, которых не ждёт.
/// </para>
/// <para>
/// <see cref="Media"/> уже разрешено: либо из самого события, либо из хранилища
/// по <c>TwitchGuid</c>/<c>TwitchPointsCost</c>. Эффект может его дополнить или
/// заменить — это единственный способ отдать награду без файла в
/// <c>TwitchCore</c>.
/// </para>
/// </remarks>
public sealed class RewardActivationContext
{
    /// <summary>Событие редемпшена, пришедшее из очереди.</summary>
    public required RewardRedeemedEvent Reward { get; init; }

    /// <summary>Медиа награды; пустой список означает «файла нет».</summary>
    public required IReadOnlyList<MediaInfo> Media { get; init; }

    public string AlertKey => Reward.AlertKey ?? string.Empty;

    public string UserId => Reward.UserId;

    public string UserName => Reward.UserName;

    public string? UserInput => Reward.UserInput;

    public int Cost => Reward.Cost;
}
