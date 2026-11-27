namespace MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;

public static class ServiceNameRegistry
{
    private static readonly Dictionary<string, string> CanonicalNames = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["MARS.Twitch"] = "TwitchCore",
        ["MARS.TwitchCore"] = "TwitchCore",
        ["Twitch"] = "TwitchCore",
        ["TwitchCore"] = "TwitchCore",
        ["MARS.Alerts"] = "Alerts",
        ["Alerts"] = "Alerts",
        ["MARS.Commands"] = "Commands",
        ["Commands"] = "Commands",
        ["MARS.Discord"] = "Discord",
        ["Discord"] = "Discord",
        ["MARS.MediaStorage"] = "MediaStorage",
        ["MediaStorage"] = "MediaStorage",
        ["MARS.Shikimori"] = "Shikimori",
        ["Shikimori"] = "Shikimori",
        ["MARS.SoundRequest"] = "SoundRequest",
        ["SoundRequest"] = "SoundRequest",
        ["MARS.Telegram"] = "Telegram",
        ["Telegram"] = "Telegram",
        ["MARS.Tts"] = "Tts",
        ["Tts"] = "Tts",
        ["MARS.WaifuGacha"] = "WaifuGacha",
        ["WaifuGacha"] = "WaifuGacha",
        ["MARS.AudioController"] = "AudioController",
        ["AudioController"] = "AudioController",
    };

    public static string Canonicalize(string value) =>
        CanonicalNames.TryGetValue(value, out var canonical) ? canonical : value;
}
