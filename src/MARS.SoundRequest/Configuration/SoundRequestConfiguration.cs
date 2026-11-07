namespace MARS.SoundRequest.Configuration;

public class SoundRequestConfiguration
{
    public const string SectionName = "SoundRequest";

    public SoundRequestProvider Provider { get; set; } = SoundRequestProvider.YouTube;
    public string[] EnabledPlatforms { get; set; } = ["YouTube", "Spotify", "SoundCloud"];
}

public enum SoundRequestProvider
{
    YouTube = 0,
    Spotify = 1,
    SoundCloud = 2,
}
