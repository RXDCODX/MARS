namespace MARS.Twitch.Api.Entities;

public class RxdcodxAvatarMissingUserDto
{
    public required string UserId { get; init; }

    public string? UserName { get; init; }

    public string? ProfileImageUrl { get; init; }

    public DateTime? LastUpdated { get; init; }
}
