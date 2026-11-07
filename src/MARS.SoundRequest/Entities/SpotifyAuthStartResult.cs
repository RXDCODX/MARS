namespace MARS.SoundRequest.Entities;

public class SpotifyAuthStartResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string AuthUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}
