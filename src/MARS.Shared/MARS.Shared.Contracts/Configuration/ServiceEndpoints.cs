namespace MARS.Shared.Contracts.Configuration;

/// <summary>
/// Адреса сервисов — общий контракт конфигурации. Свойства несут метаданные
/// для админки, поэтому реестр управляемых сервисов строится из них, а не из
/// отдельного ручного списка.
/// </summary>
public class ServiceEndpoints
{
    public const string SectionName = "ServiceEndpoints";

    [ManagedService("twitch-core", "Twitch Core", "Основной Twitch сервис")]
    public string TwitchCore { get; set; } = "http://twitch-core:8080";

    [ManagedService("waifu-gacha", "Waifu Gacha", "Сервис вайфу гача")]
    public string WaifuGacha { get; set; } = "http://waifu-gacha:8080";

    [ManagedService("shikimori", "Shikimori", "Сервис Shikimori")]
    public string Shikimori { get; set; } = "http://shikimori:8080";

    [ManagedService("telegram", "Telegram", "Telegram бот")]
    public string Telegram { get; set; } = "http://telegram:8080";

    [ManagedService("discord", "Discord", "Discord бот")]
    public string Discord { get; set; } = "http://discord:8080";

    [ManagedService("commands", "Commands", "Сервис команд")]
    public string Commands { get; set; } = "http://commands:8080";

    [ManagedService("sound-request", "Sound Request", "Сервис звуковых запросов")]
    public string SoundRequest { get; set; } = "http://sound-request:8080";

    [ManagedService("tts", "TTS", "Сервис синтеза речи")]
    public string TTS { get; set; } = "http://tts:8080";

    [ManagedService("obs", "OBS", "OBS интеграция")]
    public string OBS { get; set; } = "http://obs:8080";

    [ManagedService("alerts", "Alerts", "Сервис алертов")]
    public string Alerts { get; set; } = "http://alerts:8080";

    [ManagedService("scoreboard", "Scoreboard", "Сервис табло")]
    public string Scoreboard { get; set; } = "http://scoreboard:8080";

    [ManagedService("cinema-queue", "Cinema Queue", "Сервис очереди кинотеатра")]
    public string CinemaQueue { get; set; } = "http://cinema-queue:8080";

    [ManagedService("media-storage", "Media Storage", "Сервис хранения медиа")]
    public string MediaStorage { get; set; } = "http://media-storage:8080";

    [ManagedService("admin", "Admin", "Сервис админки")]
    public string Admin { get; set; } = "http://admin:8080";
}
