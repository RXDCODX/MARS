using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Enums;
using MARS.MediaStorage.Application.Downloads;
using MARS.MediaStorage.Domain.Entities;
using MARS.Platform.Infrastructure.SoundCloud;
using MARS.Platform.Infrastructure.YouTube;
using MARS.Shared.Contracts.MediaStorage;
using MARS.Shared.Contracts.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Wolverine;
using System.Diagnostics.CodeAnalysis;

namespace MARS.MediaStorage.Infrastructure.Media.Downloads;

[ExcludeFromCodeCoverage]
public sealed class MediaDownloadJobProcessor(
    IMediaDownloadJobRepository repository,
    IYouTubeApi youTubeApi,
    ISoundCloudApi soundCloudApi,
    DirectHttpMediaSourceDownloader directDownloader,
    IWebHostEnvironment environment,
    IConfiguration configuration,
    IServiceProvider serviceProvider,
    ILogger<MediaDownloadJobProcessor> logger
) : IMediaDownloadJobProcessor
{
    private static readonly string[] VideoExtensions = [".mp4", ".mkv", ".mov", ".webm", ".avi"];
    private const long MaxSourceBytes = 500L * 1024 * 1024;
    private const int MaxSourceMinutes = 30;

    public async Task ProcessAsync(
        MediaDownloadJob job,
        CancellationToken cancellationToken = default
    )
    {
        var startedAt = DateTimeOffset.UtcNow;
        var workingDirectory = Path.Combine(GetCacheRoot(), job.Id.ToString("N"));
        Directory.CreateDirectory(workingDirectory);

        try
        {
            job.Stage = "downloading";
            await repository.UpdateAsync(job, cancellationToken);

            var source = await DownloadSourceAsync(job, workingDirectory, cancellationToken);
            job.Title = string.IsNullOrWhiteSpace(source.Title) ? job.Title : source.Title;
            job.ArtifactDurationSeconds = source.Duration.TotalSeconds;

            var probe = await FFProbe.AnalyseAsync(
                source.SourcePath,
                cancellationToken: cancellationToken
            );
            job.ArtifactWidth = probe.PrimaryVideoStream?.Width ?? 0;
            job.ArtifactHeight = probe.PrimaryVideoStream?.Height ?? 0;
            var durationSeconds =
                probe.Duration.TotalSeconds > 0
                    ? probe.Duration.TotalSeconds
                    : source.Duration.TotalSeconds;
            if (durationSeconds <= 0)
            {
                throw new InvalidOperationException(
                    "Не удалось определить длительность исходника."
                );
            }

            if (durationSeconds > MaxSourceMinutes * 60)
            {
                throw new InvalidOperationException($"Исходник длиннее {MaxSourceMinutes} минут.");
            }

            var sourceSize = new FileInfo(source.SourcePath).Length;
            if (sourceSize > MaxSourceBytes)
            {
                throw new InvalidOperationException("Исходник больше 500 МБ.");
            }

            var hasVideo = probe.PrimaryVideoStream is not null;
            var kind = hasVideo ? MediaDownloadArtifactKind.Video : MediaDownloadArtifactKind.Audio;
            job.ArtifactKind = kind;

            job.Stage = "preparing";
            await repository.UpdateAsync(job, cancellationToken);

            var outputPath = Path.Combine(GetArtifactRoot(), $"{job.Id:N}.mp4");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            if (kind == MediaDownloadArtifactKind.Audio)
            {
                await PrepareAudioAsync(
                    source.SourcePath,
                    outputPath,
                    durationSeconds,
                    cancellationToken
                );
            }
            else
            {
                await PrepareVideoAsync(source, outputPath, durationSeconds, cancellationToken);
            }

            var outputInfo = new FileInfo(outputPath);
            if (outputInfo.Length > MediaDownloadPolicy.TargetBytes)
            {
                throw new InvalidOperationException("Не удалось уложиться в 19 МБ.");
            }

            job.Stage = "validating";
            await repository.UpdateAsync(job, cancellationToken);
            await ValidateDecodedAsync(outputPath, cancellationToken);

            var internalBaseUrl = (
                configuration["MediaStorage:InternalBaseUrl"] ?? "http://media-storage:8080"
            ).TrimEnd('/');
            job.ArtifactUrl = $"{internalBaseUrl}/api/MediaDownloadJobs/{job.Id}/artifact";
            job.ArtifactFileName = BuildFileName(
                job.Title,
                kind == MediaDownloadArtifactKind.Video ? "mp4" : "mp3"
            );
            job.ArtifactMimeType =
                kind == MediaDownloadArtifactKind.Video ? "video/mp4" : "audio/mpeg";
            job.ArtifactSizeBytes = outputInfo.Length;
            job.ArtifactStoragePath = outputPath;
            job.Caption = BuildCaption(job, outputInfo.Length, durationSeconds);
            job.Status = MediaDownloadJobStatus.Completed;
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            job.ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(
                MediaDownloadPolicy.ArtifactCacheTtlMinutes
            );
            job.Stage = "completed";
            await repository.UpdateAsync(job, cancellationToken);

            await PublishCompletedAsync(job, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            job.Status = MediaDownloadJobStatus.Cancelled;
            job.FailureCategory = "cancelled";
            job.FailureReason = "Задача отменена.";
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            job.Stage = "cancelled";
            await repository.UpdateAsync(job, cancellationToken);
            await PublishCompletedAsync(job, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Media download job {JobId} failed", job.Id);
            job.Status = MediaDownloadJobStatus.Failed;
            job.FailureCategory = "processing";
            job.FailureReason = ex.Message;
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            job.Stage = "failed";
            await repository.UpdateAsync(job, cancellationToken);
            await PublishCompletedAsync(job, cancellationToken);
        }
        finally
        {
            try
            {
                if (Directory.Exists(workingDirectory))
                {
                    Directory.Delete(workingDirectory, recursive: true);
                }
            }
            catch
            {
                // best effort
            }
        }
    }

    private async Task<MediaSourceDownloadResult> DownloadSourceAsync(
        MediaDownloadJob job,
        string workingDirectory,
        CancellationToken cancellationToken
    )
    {
        var kind = job.SourceKind;
        if (kind == "youtube")
        {
            var video = await youTubeApi.GetVideoAsync(job.Url, cancellationToken);
            if (video is null)
            {
                throw new InvalidOperationException(
                    "Не удалось получить информацию о видео YouTube."
                );
            }

            var videoStreams = await youTubeApi.GetVideoStreamsAsync(video.Id, cancellationToken);
            var audioStreams = await youTubeApi.GetAudioStreamsAsync(video.Id, cancellationToken);
            var bestVideo = videoStreams
                .OrderByDescending(x => x.Width)
                .ThenByDescending(x => x.BitsPerSecond)
                .FirstOrDefault();
            var bestAudio = audioStreams.OrderByDescending(x => x.BitsPerSecond).FirstOrDefault();
            if (bestVideo is null || bestAudio is null)
            {
                throw new InvalidOperationException("Не удалось получить потоки YouTube.");
            }

            var videoPath = Path.Combine(
                workingDirectory,
                "source_video" + GetExtension(bestVideo.Container)
            );
            var audioPath = Path.Combine(
                workingDirectory,
                "source_audio" + GetExtension(bestAudio.Container)
            );
            await youTubeApi.DownloadVideoAsync(video.Id, bestVideo, videoPath, cancellationToken);
            await youTubeApi.DownloadAsync(video.Id, bestAudio, audioPath, cancellationToken);

            return new MediaSourceDownloadResult(
                videoPath,
                audioPath,
                video.Title,
                video.Duration ?? TimeSpan.Zero,
                MediaDownloadArtifactKind.Video,
                "video/mp4"
            );
        }

        if (kind == "soundcloud")
        {
            var track = await soundCloudApi.GetTrackAsync(job.Url, cancellationToken);
            if (track is null)
            {
                throw new InvalidOperationException(
                    "Не удалось получить информацию о треке SoundCloud."
                );
            }

            var audioPath = Path.Combine(workingDirectory, "source_audio.mp3");
            await soundCloudApi.DownloadAsync(track.Url, audioPath, cancellationToken);
            return new MediaSourceDownloadResult(
                audioPath,
                null,
                track.Title,
                track.Duration ?? TimeSpan.Zero,
                MediaDownloadArtifactKind.Audio,
                "audio/mpeg"
            );
        }

        if (!configuration.GetValue("MediaStorage:DownloadJobs:AllowDirectUrls", true))
        {
            throw new InvalidOperationException("Прямые URL отключены в этом окружении.");
        }

        var directPath = Path.Combine(workingDirectory, "source" + GuessExtension(job.Url));
        var downloaded = await directDownloader.DownloadAsync(
            job.Url,
            directPath,
            cancellationToken
        );
        if (downloaded is null)
        {
            throw new InvalidOperationException("Не удалось скачать исходник.");
        }

        var probe = await FFProbe.AnalyseAsync(downloaded, cancellationToken: cancellationToken);
        var hasVideo = probe.PrimaryVideoStream is not null;
        return new MediaSourceDownloadResult(
            downloaded,
            null,
            Path.GetFileNameWithoutExtension(downloaded),
            probe.Duration,
            hasVideo ? MediaDownloadArtifactKind.Video : MediaDownloadArtifactKind.Audio,
            hasVideo ? "video/mp4" : "audio/mpeg"
        );
    }

    private async Task PrepareVideoAsync(
        MediaSourceDownloadResult source,
        string outputPath,
        double durationSeconds,
        CancellationToken cancellationToken
    )
    {
        if (source.AudioPath is null)
        {
            throw new InvalidOperationException("Отсутствует аудиопоток.");
        }

        var fastPathCompatible = IsFastPathCompatible(source.SourcePath, source.AudioPath);
        var sourceSize =
            new FileInfo(source.SourcePath).Length + new FileInfo(source.AudioPath).Length;
        if (fastPathCompatible && sourceSize <= MediaDownloadPolicy.TargetBytes)
        {
            // Mux without re-encode is not applicable for separate streams; fall through to ladder.
        }

        foreach (var height in MediaDownloadPolicy.VideoProfiles)
        {
            var audioBitrate =
                height >= 720 ? 128
                : height >= 540 ? 96
                : 64;
            var videoBitrate = CalculateVideoBitrate(durationSeconds, audioBitrate);
            if (videoBitrate < MediaDownloadPolicy.MinimumVideoBitrateKbps && height > 480)
            {
                continue;
            }

            var actualVideoBitrate =
                videoBitrate < MediaDownloadPolicy.MinimumVideoBitrateKbps
                    ? MediaDownloadPolicy.MinimumVideoBitrateKbps
                    : videoBitrate;

            var trimDuration = durationSeconds;
            if (
                actualVideoBitrate == MediaDownloadPolicy.MinimumVideoBitrateKbps
                && videoBitrate < MediaDownloadPolicy.MinimumVideoBitrateKbps
            )
            {
                trimDuration = Math.Floor(
                    (MediaDownloadPolicy.TargetBytes * 8.0)
                        / ((actualVideoBitrate + audioBitrate) * 1000.0)
                );
                if (trimDuration < 1)
                {
                    continue;
                }
            }

            var passlog = Path.Combine(
                Path.GetDirectoryName(outputPath)!,
                $"passlog-{Guid.NewGuid():N}"
            );
            try
            {
                await RunTwoPassAsync(
                    source.SourcePath,
                    source.AudioPath,
                    outputPath,
                    height,
                    actualVideoBitrate,
                    audioBitrate,
                    trimDuration,
                    passlog,
                    cancellationToken
                );
                if (new FileInfo(outputPath).Length <= MediaDownloadPolicy.TargetBytes)
                {
                    return;
                }
            }
            finally
            {
                DeletePasslog(passlog);
            }
        }

        throw new InvalidOperationException("Не удалось сжать видео до 19 МБ.");
    }

    private async Task PrepareAudioAsync(
        string sourcePath,
        string outputPath,
        double durationSeconds,
        CancellationToken cancellationToken
    )
    {
        foreach (var bitrate in MediaDownloadPolicy.AudioBitratesKbps)
        {
            await RunAudioTranscodeAsync(sourcePath, outputPath, bitrate, cancellationToken);
            if (new FileInfo(outputPath).Length <= MediaDownloadPolicy.TargetBytes)
            {
                return;
            }
        }

        throw new InvalidOperationException("Не удалось сжать аудио до 19 МБ.");
    }

    private static int CalculateVideoBitrate(double durationSeconds, int audioBitrateKbps)
    {
        if (durationSeconds <= 0)
        {
            return 0;
        }

        return (int)
            Math.Floor(
                (MediaDownloadPolicy.TargetBytes * 8.0) / (durationSeconds * 1000.0)
                    - audioBitrateKbps
            );
    }

    private async Task RunTwoPassAsync(
        string videoPath,
        string audioPath,
        string outputPath,
        int height,
        int videoBitrateKbps,
        int audioBitrateKbps,
        double trimDuration,
        string passlog,
        CancellationToken cancellationToken
    )
    {
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        var pass1 = FFMpegArguments
            .FromFileInput(videoPath)
            .AddFileInput(audioPath)
            .OutputToFile(
                OperatingSystem.IsWindows() ? "NUL" : "/dev/null",
                overwrite: true,
                options =>
                    options
                        .WithVideoCodec(VideoCodec.LibX264)
                        .WithVideoBitrate(videoBitrateKbps)
                        .WithCustomArgument("-preset veryfast")
                        .WithCustomArgument("-pix_fmt yuv420p")
                        .WithVideoFilters(filters => filters.Scale(-2, height))
                        .WithCustomArgument(
                            "-t " + trimDuration.ToString("0.###", CultureInfo.InvariantCulture)
                        )
                        .WithCustomArgument("-pass 1")
                        .WithCustomArgument("-passlogfile " + passlog)
                        .WithCustomArgument("-an")
            );

        await pass1.CancellableThrough(cancellationToken).ProcessAsynchronously();

        var pass2 = FFMpegArguments
            .FromFileInput(videoPath)
            .AddFileInput(audioPath)
            .OutputToFile(
                outputPath,
                overwrite: true,
                options =>
                    options
                        .WithCustomArgument("-map 0:v:0")
                        .WithCustomArgument("-map 1:a:0")
                        .WithVideoCodec(VideoCodec.LibX264)
                        .WithVideoBitrate(videoBitrateKbps)
                        .WithAudioCodec(AudioCodec.Aac)
                        .WithAudioBitrate(audioBitrateKbps)
                        .WithCustomArgument("-preset veryfast")
                        .WithCustomArgument("-pix_fmt yuv420p")
                        .WithVideoFilters(filters => filters.Scale(-2, height))
                        .WithFastStart()
                        .WithCustomArgument(
                            "-t " + trimDuration.ToString("0.###", CultureInfo.InvariantCulture)
                        )
                        .WithCustomArgument("-pass 2")
                        .WithCustomArgument("-passlogfile " + passlog)
                        .WithCustomArgument($"-maxrate {videoBitrateKbps}k")
                        .WithCustomArgument($"-bufsize {videoBitrateKbps * 2}k")
            );

        await pass2.CancellableThrough(cancellationToken).ProcessAsynchronously();
    }

    private async Task RunAudioTranscodeAsync(
        string sourcePath,
        string outputPath,
        int bitrateKbps,
        CancellationToken cancellationToken
    )
    {
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        await FFMpegArguments
            .FromFileInput(sourcePath)
            .OutputToFile(
                outputPath,
                overwrite: true,
                options =>
                    options
                        .WithAudioCodec(AudioCodec.LibMp3Lame)
                        .WithAudioBitrate(bitrateKbps)
                        .WithCustomArgument("-map_metadata -1")
            )
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();
    }

    private async Task ValidateDecodedAsync(string path, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-v error -xerror -i \"{path}\" -f null -",
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var process = Process.Start(psi)!;
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Файл не прошёл проверку декодирования: {stderr}");
        }
    }

    private async Task PublishCompletedAsync(
        MediaDownloadJob job,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var bus = serviceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(
                new MediaDownloadJobCompletedEvent
                {
                    JobId = job.Id,
                    Succeeded = job.Status == MediaDownloadJobStatus.Completed,
                    FailureCategory = job.FailureCategory,
                    FailureReason = job.FailureReason,
                    ArtifactUrl = job.ArtifactUrl,
                    FileName = job.ArtifactFileName,
                    MimeType = job.ArtifactMimeType,
                    Kind = (int)job.ArtifactKind,
                    SizeBytes = job.ArtifactSizeBytes,
                    DurationSeconds = job.ArtifactDurationSeconds,
                    Width = job.ArtifactWidth,
                    Height = job.ArtifactHeight,
                    Trimmed = job.Trimmed,
                    Caption = job.Caption,
                    CompletedAtUtc = job.CompletedAtUtc ?? DateTimeOffset.UtcNow,
                }
            );
            job.Stage = (job.Status == MediaDownloadJobStatus.Completed ? "published" : job.Stage);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish completion event for job {JobId}", job.Id);
        }
    }

    private string GetCacheRoot()
    {
        var configured = configuration["MediaStorage:DownloadJobs:CachePath"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return Path.Combine(environment.ContentRootPath, "media-downloads");
    }

    private string GetArtifactRoot() => Path.Combine(GetCacheRoot(), "artifacts");

    private static bool IsFastPathCompatible(string videoPath, string audioPath) => false;

    private static string GetExtension(string container) =>
        string.IsNullOrWhiteSpace(container) ? ".bin" : "." + container.TrimStart('.');

    private static string GuessExtension(string url)
    {
        try
        {
            var uri = new Uri(url);
            var ext = Path.GetExtension(uri.AbsolutePath);
            return string.IsNullOrWhiteSpace(ext) ? ".bin" : ext;
        }
        catch
        {
            return ".bin";
        }
    }

    private static string BuildFileName(string title, string extension)
    {
        var safe = new string(
            title.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray()
        ).Trim();
        if (safe.Length > 64)
        {
            safe = safe[..64].Trim();
        }

        if (string.IsNullOrWhiteSpace(safe))
        {
            safe = "download";
        }

        return $"{safe}_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.{extension}";
    }

    private static string BuildCaption(MediaDownloadJob job, long sizeBytes, double durationSeconds)
    {
        var sizeMb = sizeBytes / 1_000_000.0;
        var trimmed = job.Trimmed ? " (обрезано)" : string.Empty;
        var resolution =
            job.ArtifactWidth > 0 && job.ArtifactHeight > 0
                ? $"{job.ArtifactWidth}x{job.ArtifactHeight}"
                : "audio";
        return $"Имя файла: {job.ArtifactFileName}\nРазмер: {sizeMb:0.00} МБ\nРазрешение: {resolution}{trimmed}";
    }

    private static void DeletePasslog(string passlog)
    {
        try
        {
            foreach (
                var file in Directory.GetFiles(
                    Path.GetDirectoryName(passlog)!,
                    Path.GetFileName(passlog) + "*"
                )
            )
            {
                File.Delete(file);
            }
        }
        catch
        {
            // best effort
        }
    }
}
