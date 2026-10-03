using System.Diagnostics;
using System.Text.RegularExpressions;
using MARS.Discord.Models;

namespace MARS.Discord.Services.YouTube;

public class YouTubeResolver(IYouTubeApi api, ILogger<YouTubeResolver> logger) : IYouTubeResolver
{
    public async Task<BaseTrackInfo[]> SearchTracksAsync(
        string query,
        int maxResults,
        CancellationToken ct
    )
    {
        BaseTrackInfo[] result = [];

        if (!string.IsNullOrWhiteSpace(query) && maxResults > 0)
        {
            try
            {
                var videos = await api.SearchVideosAsync(query, maxResults, ct);

                result = [.. videos.Select(CreateTrackInfo)];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[YouTubeResolver] Ошибка поиска по запросу {Query}", query);
            }
        }

        return result;
    }

    public async Task<BaseTrackInfo?> ResolveVideoAsync(string url, CancellationToken ct)
    {
        BaseTrackInfo? result = null;

        if (!string.IsNullOrWhiteSpace(url))
        {
            try
            {
                var video = await api.GetVideoAsync(url, ct);

                if (video is not null)
                {
                    result = CreateTrackInfo(video);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[YouTubeResolver] Ошибка резолва видео {Url}", url);
            }
        }

        return result;
    }

    public async Task<string?> DownloadBestAudioStreamAsync(
        BaseTrackInfo? track,
        string outputDirectory,
        CancellationToken ct
    )
    {
        string? result = null;

        if (track is not null && !string.IsNullOrWhiteSpace(outputDirectory))
        {
            var videoId = await ResolveDownloadVideoIdAsync(track, ct);
            if (!string.IsNullOrWhiteSpace(videoId))
            {
                Directory.CreateDirectory(outputDirectory);

                result = await TryDownloadWithYoutubeReExplodeAsync(
                    track,
                    videoId,
                    outputDirectory,
                    ct
                );

                if (string.IsNullOrWhiteSpace(result))
                {
                    result = await TryDownloadWithYtDlpAsync(track, videoId, outputDirectory, ct);
                }

                if (string.IsNullOrWhiteSpace(result))
                {
                    logger.LogWarning(
                        "[YouTubeResolver] Не удалось скачать аудио для videoId={VideoId}. URL={Url}",
                        videoId,
                        track.Url
                    );
                }
            }
            else
            {
                logger.LogWarning(
                    "[YouTubeResolver] Не удалось определить video id для URL={Url}",
                    track.Url
                );
            }
        }

        return result;
    }

    public string? GetVideoId(BaseTrackInfo track)
    {
        string? result = null;

        if (track is not null)
        {
            if (!string.IsNullOrWhiteSpace(track.VideoId))
            {
                result = track.VideoId;
            }
            else if (track.Url is not null)
            {
                result = TryExtractVideoId(track.Url.ToString());
            }
        }

        return result;
    }

    private async Task<string?> ResolveDownloadVideoIdAsync(
        BaseTrackInfo track,
        CancellationToken ct
    )
    {
        var result = GetVideoId(track);

        if (track.Url is not null)
        {
            try
            {
                var video = await api.GetVideoAsync(track.Url.ToString(), ct);
                if (!string.IsNullOrWhiteSpace(video?.Id))
                {
                    result = video.Id;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "[YouTubeResolver] Не удалось обновить video id через Videos.GetAsync для URL={Url}",
                    track.Url
                );
            }
        }

        return result;
    }

    private async Task<string?> TryDownloadWithYoutubeReExplodeAsync(
        BaseTrackInfo track,
        string videoId,
        string outputDirectory,
        CancellationToken ct
    )
    {
        string? result = null;

        for (var attempt = 1; attempt <= 2 && string.IsNullOrWhiteSpace(result); attempt++)
        {
            try
            {
                var streams = await api.GetAudioStreamsAsync(videoId, ct);
                var streamInfo = SelectBestStream(streams);

                if (streamInfo is not null)
                {
                    var fileName = string.Concat(
                        BuildSafeFileName(track.Title, videoId),
                        ".",
                        GetStreamExtension(streamInfo)
                    );
                    var filePath = Path.Combine(outputDirectory, fileName);

                    await api.DownloadAsync(videoId, streamInfo, filePath, ct);

                    if (File.Exists(filePath))
                    {
                        result = filePath;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "[YouTubeResolver] Попытка {Attempt} скачать через YoutubeReExplode завершилась ошибкой для videoId={VideoId}",
                    attempt,
                    videoId
                );
            }
        }

        return result;
    }

    private async Task<string?> TryDownloadWithYtDlpAsync(
        BaseTrackInfo track,
        string videoId,
        string outputDirectory,
        CancellationToken ct
    )
    {
        string? result = null;
        var videoUrl = BuildVideoUrl(track, videoId);

        if (!string.IsNullOrWhiteSpace(videoUrl))
        {
            var safeBaseName = BuildSafeFileName(track.Title, videoId);
            var outputTemplate = Path.Combine(
                outputDirectory,
                string.Concat(safeBaseName, ".%(ext)s")
            );
            var existingFiles = Directory
                .GetFiles(outputDirectory, string.Concat(safeBaseName, ".*"))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var process = new Process();

                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                };

                process.StartInfo.ArgumentList.Add("--no-playlist");
                process.StartInfo.ArgumentList.Add("-f");
                process.StartInfo.ArgumentList.Add("bestaudio/best");
                process.StartInfo.ArgumentList.Add("-o");
                process.StartInfo.ArgumentList.Add(outputTemplate);
                process.StartInfo.ArgumentList.Add(videoUrl);

                if (process.Start())
                {
                    var standardErrorTask = process.StandardError.ReadToEndAsync(ct);
                    var standardOutputTask = process.StandardOutput.ReadToEndAsync(ct);

                    await process.WaitForExitAsync(ct);

                    var standardError = await standardErrorTask;
                    await standardOutputTask;

                    if (process.ExitCode == 0)
                    {
                        result = FindDownloadedFile(outputDirectory, safeBaseName, existingFiles);
                    }
                    else
                    {
                        logger.LogWarning(
                            "[YouTubeResolver] yt-dlp завершился с кодом {ExitCode} для videoId={VideoId}. stderr: {StandardError}",
                            process.ExitCode,
                            videoId,
                            standardError
                        );
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                logger.LogWarning(
                    "[YouTubeResolver] yt-dlp не найден в PATH. videoId={VideoId}",
                    videoId
                );
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "[YouTubeResolver] Ошибка fallback-загрузки через yt-dlp для videoId={VideoId}",
                    videoId
                );
            }
        }

        return result;
    }

    /// <summary>
    /// Сначала выбирается аудио без видео: оно меньше, а качество выше. Среди
    /// muxed-потоков остаётся запасной вариант, когда аудио нет.
    /// </summary>
    private static YouTubeAudioStream? SelectBestStream(IReadOnlyList<YouTubeAudioStream> streams)
    {
        YouTubeAudioStream? result = streams
            .Where(stream => stream.IsAudioOnly)
            .OrderByDescending(stream => stream.BitsPerSecond)
            .FirstOrDefault();

        result ??= streams
            .Where(stream => !stream.IsAudioOnly)
            .OrderByDescending(stream => stream.BitsPerSecond)
            .FirstOrDefault();

        return result;
    }

    private static string BuildVideoUrl(BaseTrackInfo track, string videoId)
    {
        var result = !string.IsNullOrWhiteSpace(track?.Url?.ToString())
            ? track.Url.ToString()
            : string.Concat("https://www.youtube.com/watch?v=", videoId);

        return result;
    }

    private static string? FindDownloadedFile(
        string outputDirectory,
        string safeBaseName,
        HashSet<string> existingFiles
    )
    {
        string? result = null;

        var candidates = Directory
            .GetFiles(outputDirectory, string.Concat(safeBaseName, ".*"))
            .Where(path =>
                !path.EndsWith(".part", StringComparison.OrdinalIgnoreCase)
                && !path.EndsWith(".ytdl", StringComparison.OrdinalIgnoreCase)
            )
            .Where(path => !existingFiles.Contains(path))
            .OrderByDescending(path => File.GetLastWriteTimeUtc(path))
            .ToArray();

        if (candidates.Length == 0)
        {
            candidates = Directory
                .GetFiles(outputDirectory, string.Concat(safeBaseName, ".*"))
                .Where(path =>
                    !path.EndsWith(".part", StringComparison.OrdinalIgnoreCase)
                    && !path.EndsWith(".ytdl", StringComparison.OrdinalIgnoreCase)
                )
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToArray();
        }

        if (candidates.Length > 0)
        {
            result = candidates[0];
        }

        return result;
    }

    private static BaseTrackInfo CreateTrackInfo(YouTubeVideoInfo video)
    {
        string[] authors = !string.IsNullOrWhiteSpace(video.Author) ? [video.Author] : [];
        var result = new BaseTrackInfo
        {
            Id = Guid.NewGuid(),
            Url = new Uri(video.Url),
            VideoId = video.Id,
            TrackName = video.Title,
            Authors = authors,
            Duration = video.Duration ?? TimeSpan.Zero,
            ArtworkUrl = !string.IsNullOrWhiteSpace(video.ThumbnailUrl)
                ? new Uri(video.ThumbnailUrl)
                : null,
        };

        return result;
    }

    /// <summary>
    /// Аудио в контейнере mp4 сохраняется как m4a: расширение должно совпадать с
    /// содержимым, иначе плеер не отдаст файл в очередь.
    /// </summary>
    private static string GetStreamExtension(YouTubeAudioStream streamInfo)
    {
        var result = streamInfo.Container;

        if (
            streamInfo.IsAudioOnly
            && string.Equals(streamInfo.Container, "mp4", StringComparison.OrdinalIgnoreCase)
        )
        {
            result = "m4a";
        }

        return result;
    }

    /// <summary>
    /// Символы, недопустимые в имени файла, объединённые для всех ОС.
    /// <see cref="Path.GetInvalidFileNameChars"/> на Linux знает только про NUL и
    /// «/» и пропустил бы «?», «:» и кавычку, а такой файл не открывается на
    /// Windows. Поэтому запрещённое берётся не с текущей ОС, а из константы.
    /// </summary>
    private static readonly char[] ForbiddenFileNameChars =
    [
        .. Path.GetInvalidFileNameChars().Concat("\\\"<>|:?*"),
    ];

    private static string BuildSafeFileName(string title, string fallback)
    {
        var result = fallback;

        if (!string.IsNullOrWhiteSpace(title))
        {
            var safeChars = title.Select(character =>
                Array.IndexOf(ForbiddenFileNameChars, character) >= 0 ? '_' : character
            );
            var normalizedTitle = new string(safeChars.ToArray()).Trim();
            if (normalizedTitle.Length > 80)
            {
                normalizedTitle = normalizedTitle[..80].Trim();
            }

            if (!string.IsNullOrWhiteSpace(normalizedTitle))
            {
                result = normalizedTitle;
            }
        }

        return result;
    }

    private static string? TryExtractVideoId(string url)
    {
        string? result = null;

        if (!string.IsNullOrWhiteSpace(url))
        {
            var match = Regex.Match(
                url,
                @"(?:youtu\.be/|youtube\.com/(?:watch\?v=|shorts/|embed/))([^?&/]+)",
                RegexOptions.IgnoreCase
            );

            if (match.Success)
            {
                result = match.Groups[1].Value;
            }
        }

        return result;
    }
}
