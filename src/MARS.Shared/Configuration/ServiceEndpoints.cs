namespace MARS.Shared.Configuration;

public class ServiceEndpoints
{
    public const string SectionName = "ServiceEndpoints";

    public string TwitchCore { get; set; } = "http://twitch-core:8080";
    public string WaifuGacha { get; set; } = "http://waifu-gacha:8080";
    public string Shikimori { get; set; } = "http://shikimori:8080";
    public string Telegram { get; set; } = "http://telegram:8080";
    public string Discord { get; set; } = "http://discord:8080";
    public string Commands { get; set; } = "http://commands:8080";
    public string SoundRequest { get; set; } = "http://sound-request:8080";
    public string TTS { get; set; } = "http://tts:8080";
    public string OBS { get; set; } = "http://obs:8080";
    public string Alerts { get; set; } = "http://alerts:8080";
    public string Scoreboard { get; set; } = "http://scoreboard:8080";
    public string CinemaQueue { get; set; } = "http://cinema-queue:8080";
    public string MediaStorage { get; set; } = "http://media-storage:8080";
    public string Admin { get; set; } = "http://admin:8080";
}
