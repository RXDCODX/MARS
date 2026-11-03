using MARS.Shared.Models.Media;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Что эффект награды добавил к активации.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Suppress"/> — «дальше ничего не отправляй». Нужен там, где
/// серверная часть уже отрисовала сама (рикролл) либо показать нечего
/// (награда закрывает игру, по тегу ничего не нашлось, кулдаун MIKU MIKU BEAM
/// ещё идёт). Без флага клиент получил бы событие и нарисовал пустой экран.
/// </para>
/// <para>
/// <see cref="Data"/> — серверное обогащение: участники луча, цвет зрителя,
/// параметры экранных эффектов. Клиент читает его вместе с
/// <c>alertKey</c>; сам эффект ничего не рендерит.
/// </para>
/// </remarks>
public sealed class RewardEffectResult
{
    /// <summary>Ничего не добавил, активацию отправлять как есть.</summary>
    public static RewardEffectResult Passthrough { get; } = new();

    /// <summary>Показать нечего, активацию не отправлять.</summary>
    public static RewardEffectResult Suppressed { get; } = new() { Suppress = true };

    public bool Suppress { get; init; }

    /// <summary>Медиа, заменяющее (или дополняющее) разрешённое контекстом.</summary>
    public IReadOnlyList<MediaInfo>? Media { get; init; }

    public object? Data { get; init; }
}
