namespace MARS.Discord.Services.YouTube;

/// <summary>
/// Аудиопоток видео: контейнер и битрейт нужны, чтобы выбрать лучший поток.
/// </summary>
/// <param name="IsAudioOnly">Аудио без видео предпочтительнее muxed-потока.</param>
public sealed record YouTubeAudioStream(string Container, long BitsPerSecond, bool IsAudioOnly);
