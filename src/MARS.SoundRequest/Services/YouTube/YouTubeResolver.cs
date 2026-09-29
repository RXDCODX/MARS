using MARS.SoundRequest.Entities;

namespace MARS.SoundRequest.Services.YouTube;

public class YouTubeResolver(ILogger<YouTubeResolver> logger, IHttpClientFactory httpClientFactory)
{
    public async Task<BaseTrackInfo?> ResolveVideoAsync(string url, CancellationToken ct)
    {
        BaseTrackInfo? result = null;

        try
        {
            var videoId = ExtractVideoId(url);
            if (!string.IsNullOrWhiteSpace(videoId))
            {
                // YouTube oEmbed API for basic metadata
                // Аудит: клиент создавался на каждый вызов — берём его у фабрики.
                using var http = httpClientFactory.CreateClient("youtube-oembed");
                var oembedUrl = $"https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v={videoId}&format=json";
                using var response = await http.GetAsync(oembedUrl, ct);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var title = root.TryGetProperty("title", out var titleProp)
                        ? titleProp.GetString()
                        : "Unknown";

                    var author = root.TryGetProperty("author_name", out var authorProp)
                        ? authorProp.GetString()
                        : "";

                    var trackName = !string.IsNullOrWhiteSpace(author) ? $"{author} - {title}" : title;

                    Uri.TryCreate($"https://www.youtube.com/watch?v={videoId}", UriKind.Absolute, out var trackUri);

                    result = new BaseTrackInfo
                    {
                        Id = Guid.CreateVersion7(DateTime.Now),
                        VideoId = videoId,
                        TrackName = trackName ?? "Unknown",
                        Url = trackUri!,
                        Duration = TimeSpan.Zero, // Duration not available from oEmbed
                        CreatedAt = DateTime.Now,
                    };
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при разрешении YouTube видео");
        }

        return result;
    }

    public async Task<BaseTrackInfo?> ResolveQueryAsync(string query, CancellationToken ct)
    {
        // YouTube search requires API key - simplified implementation
        logger.LogWarning("YouTube search by query not implemented without API key");
        await Task.CompletedTask;
        return null;
    }

    public async Task<BaseTrackInfo[]?> ResolvePlaylistAsync(string playlistUrl)
    {
        // YouTube playlist resolution requires API key
        logger.LogWarning("YouTube playlist resolution not implemented without API key");
        await Task.CompletedTask;
        return null;
    }

    public async Task<BaseTrackInfo?> ResolvePlaylistQueryAsync(
        string query,
        int maxTracks,
        CancellationToken ct
    )
    {
        logger.LogWarning("YouTube playlist query resolution not implemented without API key");
        await Task.CompletedTask;
        return null;
    }

    public static string? ExtractVideoId(string url)
    {
        string? result = null;

        if (!string.IsNullOrWhiteSpace(url))
        {
            try
            {
                var uri = new Uri(url);

                if (
                    uri.Host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
                    && uri.AbsolutePath.Contains("/watch", StringComparison.OrdinalIgnoreCase)
                )
                {
                    var query = uri.Query.TrimStart('?');
                    var parameters = query.Split('&');
                    foreach (var param in parameters)
                    {
                        var keyValue = param.Split('=');
                        if (keyValue is ["v", _])
                        {
                            result = keyValue[1];
                            break;
                        }
                    }
                }
                else if (uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
                {
                    result = uri.AbsolutePath.TrimStart('/').Split('/')[0];
                }
                else if (
                    uri.Host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
                    && uri.AbsolutePath.Contains("/embed/", StringComparison.OrdinalIgnoreCase)
                )
                {
                    result = uri.AbsolutePath.Split('/').LastOrDefault();
                }
                else if (
                    uri.Host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
                    && uri.AbsolutePath.Contains("/v/", StringComparison.OrdinalIgnoreCase)
                )
                {
                    result = uri.AbsolutePath.Split('/').LastOrDefault();
                }
            }
            catch
            {
                // Ignore URL parsing errors
            }
        }

        return result;
    }
}
