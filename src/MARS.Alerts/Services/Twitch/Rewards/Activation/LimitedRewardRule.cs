namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Правило награды с лимитами: один раз за стрим на зрителя и общий кулдаун.
/// </summary>
/// <remarks>
/// <see cref="DisplayName"/> попадает в сообщения чата и совпадает с названием
/// награды в панели Twitch, а <see cref="ActivatedText"/> — с фразой монолита,
/// чтобы зритель не заметил переезда на микросервисы.
/// </remarks>
public sealed record LimitedRewardRule(
    string AlertKey,
    string DisplayName,
    string ActivatedText,
    TimeSpan GlobalCooldown
);
