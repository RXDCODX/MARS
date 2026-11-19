namespace MARS.Twitch.Api.Entities;

/// <summary>
/// Пользователь без аватарки в отладочном ответе.
/// </summary>
public class RxdcodxAvatarMissingUserDto
{
    public required string UserId { get; init; }

    public string? UserName { get; init; }

    public string? ProfileImageUrl { get; init; }

    public DateTime? LastUpdated { get; init; }
}

/// <summary>
/// Отладочная информация о зрителях канала без аватарок.
/// </summary>
public class RxdcodxAvatarsDebugDto
{
    public int UsersWithoutAvatarsCount { get; init; }

    public List<RxdcodxAvatarMissingUserDto> UsersWithoutAvatars { get; init; } = [];

    public string Message { get; init; } = string.Empty;
}
