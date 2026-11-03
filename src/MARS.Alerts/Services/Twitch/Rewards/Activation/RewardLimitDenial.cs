namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>Почему награда не активирована.</summary>
public enum RewardLimitDenial
{
    /// <summary>Лимиты не нарушены.</summary>
    None,

    /// <summary>Зритель уже активировал награду в этом стриме.</summary>
    AlreadyActivatedThisStream,

    /// <summary>Идёт общий кулдаун награды.</summary>
    GlobalCooldown,
}
