namespace MARS.Twitch.Application.Models;

/// <summary>
/// Текущий пользователь, которому принадлежит действующий токен Twitch.
/// </summary>
public sealed class TwitchConsoleIdentity
{
    public string UserId { get; set; } = string.Empty;

    public string Login { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? ProfileImageUrl { get; set; }
}
