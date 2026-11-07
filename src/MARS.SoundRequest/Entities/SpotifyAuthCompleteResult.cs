namespace MARS.SoundRequest.Entities;

public class SpotifyAuthCompleteResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Product { get; set; }
}
