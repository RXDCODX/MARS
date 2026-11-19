namespace MARS.Twitch.Api.Entities;

public class RxdcodxAvatarsDebugDto
{
    public int UsersWithoutAvatarsCount { get; init; }

    public List<RxdcodxAvatarMissingUserDto> UsersWithoutAvatars { get; init; } = [];

    public string Message { get; init; } = string.Empty;
}
