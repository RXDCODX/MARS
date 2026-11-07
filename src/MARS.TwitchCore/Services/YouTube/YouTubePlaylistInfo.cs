namespace MARS.TwitchCore.Services.YouTube;

/// <summary>
/// Плейлист, найденный по запросу.
/// </summary>
public sealed record YouTubePlaylistInfo(string Id, string Title, string? Author);
