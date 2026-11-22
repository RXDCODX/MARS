namespace MARS.Shared.Contracts.Twitch.Rest;

/// <summary>
/// Контрактная модель пользователя Twitch для межсервисных REST-вызовов.
/// Живёт у сервиса-владельца (MARS.Twitch), потребители (например MARS.Alerts)
/// добавляют ссылку на этот проект и используют клиент из Infrastructure.Http.
/// </summary>
public sealed record TwitchUserRestModel
{
    public string TwitchId { get; init; } = string.Empty;
    public string UserLogin { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? ProfileImageUrl { get; init; }
    public string? ChatColor { get; init; }
    public bool IsModerator { get; init; }
    public bool IsVip { get; init; }
}
