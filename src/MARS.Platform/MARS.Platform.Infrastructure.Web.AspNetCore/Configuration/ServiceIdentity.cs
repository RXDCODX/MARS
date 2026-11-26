using System.Reflection;

namespace MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;

public interface IServiceIdentity
{
    string LogicalName { get; }

    string CanonicalName { get; }

    bool IsSelf(string serviceKey);
}

/// <summary>
/// Логический ключ текущего сервиса. Он приходит из AddMarsDefaults, потому что
/// адреса host/container и namespace проекта ненадёжны (например TwitchCore).
/// </summary>
public sealed record ServiceIdentity(string LogicalName, Assembly? EntryAssembly = null) : IServiceIdentity
{
    public string NamespaceServiceName { get; } = ResolveNamespaceServiceName(EntryAssembly);

    public string CanonicalName { get; } = ServiceNameRegistry.Canonicalize(
        ResolveNamespaceServiceName(EntryAssembly) is { Length: > 0 } namespaceName
            ? namespaceName
            : LogicalName
    );

    public bool IsSelf(string serviceKey) =>
        string.Equals(CanonicalName, ServiceNameRegistry.Canonicalize(serviceKey), StringComparison.Ordinal);

    private static string ResolveNamespaceServiceName(Assembly? assembly)
    {
        var name = assembly?.GetName().Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var segments = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 ? $"{segments[0]}.{segments[1]}" : name;
    }
}

public static class ServiceNameRegistry
{
    private static readonly Dictionary<string, string> CanonicalNames = new(StringComparer.OrdinalIgnoreCase)
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