using MARS.Discord.Models;

namespace MARS.Discord.Services.YouTube;

/// <summary>
/// Поиск и скачивание с YouTube для Discord.
/// </summary>
/// <remarks>
/// Интерфейс существует из-за YoutubeExplode: <see cref="YouTubeResolver"/>
/// создаёт <c>YoutubeClient</c> в конструкторе, а тот ходит в сеть уже при
/// создании. Пока потребители зависели от класса, любой их тест уводил прогон в
/// сеть и намертво зависал. Через интерфейс собирается заглушка, и проверяется
/// логика выбора трека, а не скачивание видео.
/// </remarks>
public interface IYouTubeResolver
{
    Task<BaseTrackInfo[]> SearchTracksAsync(string query, int maxResults, CancellationToken ct);

    Task<BaseTrackInfo?> ResolveVideoAsync(string url, CancellationToken ct);

    Task<string?> DownloadBestAudioStreamAsync(
        BaseTrackInfo track,
        string outputDirectory,
        CancellationToken ct
    );

    string? GetVideoId(BaseTrackInfo track);
}
