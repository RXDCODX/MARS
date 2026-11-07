namespace MARS.SoundRequest.Entities;

public class SpotifyAuthCredentials
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAtUtc { get; set; } = DateTime.UnixEpoch;
    public string DeviceId { get; set; } = string.Empty;
}
