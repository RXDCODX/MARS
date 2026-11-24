namespace MARS.Shared.Contracts.SoundRequest;

public class TrackInfo
{
    public string Id { get; set; } = "";
    public string TrackName { get; set; } = "";
    public List<string> Authors { get; set; } = new();
    public long DurationSeconds { get; set; }
    public string Url { get; set; } = "";
    public string LastTimePlays { get; set; } = "";
    public string ArtworkUrl { get; set; } = "";
    public string VideoId { get; set; } = "";
    public bool IsDeleted { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}
