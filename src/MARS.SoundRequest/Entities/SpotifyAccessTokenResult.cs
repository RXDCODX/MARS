namespace MARS.SoundRequest.Entities;

public class SpotifyAccessTokenResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UnixEpoch;
    public string DeviceId { get; set; } = string.Empty;
}
