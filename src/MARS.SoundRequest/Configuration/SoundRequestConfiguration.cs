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

public class SpotifySoundRequestConfiguration
{
    public const string SectionName = "Spotify";

    public bool Enabled { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public bool ForceDeviceTransfer { get; set; } = true;
    public string Market { get; set; } = "RU";
    public int PollingIntervalMs { get; set; } = 1500;
    public bool PrioritizeUserPlayback { get; set; } = true;
    public int UserPlaybackPriorityGraceMs { get; set; } = 5000;
}
