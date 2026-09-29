namespace MARS.SoundRequest.Services.Spotify;

public class SpotifyPlaybackSnapshot
{
    public string? TrackId { get; set; }
    public int ProgressMs { get; set; }
    public int DurationMs { get; set; }
    public bool IsPlaying { get; set; }
}
