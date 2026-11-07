namespace MARS.SoundRequest.Entities;

public class SpotifyAuthStatusResult
{
    public bool IsLinked { get; set; }
    public bool HasClientCredentials { get; set; }
    public string? DisplayName { get; set; }
    public string? UserId { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Product { get; set; }
    public string? DeviceId { get; set; }
    public DateTime? AccessTokenExpiresAtUtc { get; set; }
    public string Message { get; set; } = string.Empty;
}
