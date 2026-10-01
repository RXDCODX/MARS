using System.Security.Cryptography;
using System.Text;
using FFMpegCore;
using MARS.MediaStorage.Entities;
using MARS.MediaStorage.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using MARS.Shared.Models.Media;

namespace MARS.MediaStorage.Services.Media;

public class MediaTranscoder(
    IWebHostEnvironment env,
    ILogger<MediaTranscoder> logger,
    IMediaInspector inspector
) : IMediaTranscoder
{
    private const int MinimumAudioBitrateKbps = 128;
    private const int MinimumVideoBitrateKbps = 128;

    public async Task<string> EnsurePlayableAsync(
        string sourceFullPath,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sourceFullPath) || !File.Exists(sourceFullPath))
            {
                return sourceFullPath;
            }

            var extension = Path.GetExtension(sourceFullPath);
            var mediaType = await extension.GetFileMediaTypeAsync();

            if (mediaType != MediaType.Video)
            {
                return sourceFullPath;
            }

            var probe = await inspector.ProbeAsync(sourceFullPath, cancellationToken);
            var needs = NeedsTranscoding(mediaType, probe);

            if (!needs)
            {
                return sourceFullPath;
            }

            var cacheFilePath = GetTranscodedCachePath(sourceFullPath);

            if (IsTranscodedVersionReady(sourceFullPath, cacheFilePath))
            {
                await SyncDevelopmentCopyAsync(sourceFullPath, cacheFilePath);
                return cacheFilePath;
            }

            logger.LogInformation(
                GetConversionMessage(sourceFullPath, mediaType, probe.BitrateKbps)
            );

            var tempFile = GetCacheTempFilePath(sourceFullPath, mediaType);
            var transcodeSucceeded = await ConvertVideoAsync(
                sourceFullPath,
                tempFile,
                cancellationToken
            );

            if (!transcodeSucceeded)
            {
                return sourceFullPath;
            }

            EnsureDirectoryExists(Path.GetDirectoryName(cacheFilePath));

            // Аудит №13: исходник больше не удаляется. Результат уходит в кэш,
            // путь кэша детерминирован, поэтому повторный вызов вернёт тот же
            // файл, а оригинал останется на месте.
            File.Move(tempFile, cacheFilePath, true);
            File.SetLastWriteTimeUtc(cacheFilePath, File.GetLastWriteTimeUtc(sourceFullPath));

            await SyncDevelopmentCopyAsync(sourceFullPath, cacheFilePath);

            return cacheFilePath;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Transcode failed for {File}", sourceFullPath);
            return sourceFullPath;
        }
    }

    private static bool IsTranscodedVersionReady(string sourceFilePath, string cacheFilePath)
    {
        return MediaTranscodePathPolicy.ShouldReuseCache(
            File.Exists(cacheFilePath),
            File.Exists(cacheFilePath) ? File.GetLastWriteTimeUtc(cacheFilePath) : DateTime.MinValue,
            File.GetLastWriteTimeUtc(sourceFilePath)
        );
    }

    private string GetTranscodedCachePath(string sourceFilePath)
    {
        var root = Path.GetDirectoryName(Path.GetFullPath(sourceFilePath));

        if (string.IsNullOrWhiteSpace(root))
        {
            root = env.WebRootPath;
        }

        return MediaTranscodePathPolicy.GetTranscodedCachePath(
            sourceFilePath,
            Path.Combine(root, MediaTranscodePathPolicy.ConvertedFolderName)
        );
    }

    private string GetCacheTempFilePath(string sourceFilePath, MediaType mediaType)
    {
        // Аудит: временный файл писался в WebRootPath/Alerts/_converted, а итоговый
        // кэш — в _converted рядом с источником. При переносе файлов в подпапки это
        // давало два разных корня. Используем тот же каталог, что и итоговый кэш.
        var cacheDirectory = Path.GetDirectoryName(GetTranscodedCachePath(sourceFilePath));
        Directory.CreateDirectory(cacheDirectory ?? env.WebRootPath);

        var key = string.Join(
            '|',
            sourceFilePath,
            File.GetLastWriteTimeUtc(sourceFilePath).Ticks,
            mediaType,
            MinimumAudioBitrateKbps,
            MinimumVideoBitrateKbps
        );
        var hash = Convert
            .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))
            .ToLowerInvariant();

        var cacheRoot = cacheDirectory ?? env.WebRootPath;
        var extension = Path.GetExtension(sourceFilePath);

        return Path.Combine(cacheRoot, hash + extension);
    }

    private static string GetConversionMessage(
        string sourceFilePath,
        MediaType mediaType,
        long? bitrateKbps
    )
    {
        var fileName = Path.GetFileName(sourceFilePath);
        var bitrateText = bitrateKbps is > 0 ? $"{bitrateKbps} kbps" : "unknown bitrate";
        var mediaKindText = "видео";
        var result = $"Перекодирую {mediaKindText} {fileName} ({bitrateText}) для Chrome";

        return result;
    }

    private async Task SyncDevelopmentCopyAsync(string sourceFilePath, string targetFilePath)
    {
        try
        {
            // Аудит: TryGetMirroredRandomMemePath всегда возвращал null, поэтому
            // синхронизация dev-копии была no-op. Теперь путь вычисляется, но
            // только в Development — в Production wwwroot и рабочий каталог совпадают.
            var isDevelopment = env.IsDevelopment();
            var mirroredSourcePath = MediaTranscodePathPolicy.GetMirroredWebRootPath(
                sourceFilePath,
                env.WebRootPath,
                isDevelopment
            );
            var mirroredTargetPath = MediaTranscodePathPolicy.GetMirroredWebRootPath(
                targetFilePath,
                env.WebRootPath,
                isDevelopment
            );

            if (!string.IsNullOrWhiteSpace(mirroredTargetPath) && File.Exists(targetFilePath))
            {
                EnsureDirectoryExists(Path.GetDirectoryName(mirroredTargetPath));
                File.Copy(targetFilePath, mirroredTargetPath, true);
                File.SetLastWriteTimeUtc(
                    mirroredTargetPath,
                    File.GetLastWriteTimeUtc(targetFilePath)
                );

                if (
                    !string.IsNullOrWhiteSpace(mirroredSourcePath)
                    && !string.Equals(
                        mirroredSourcePath,
                        mirroredTargetPath,
                        StringComparison.OrdinalIgnoreCase
                    )
                    && File.Exists(mirroredSourcePath)
                )
                {
                    // Устаревшая dev-копия исходника больше не нужна: оригинал
                    // теперь всегда остаётся на диске.
                    File.Delete(mirroredSourcePath);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(
                ex,
                "Не удалось синхронизировать dev-копию медиафайла {FilePath}",
                targetFilePath
            );
        }

        await Task.CompletedTask;
    }

    private static bool NeedsTranscoding(
        MediaType mediaType,
        (long? BitrateKbps, double? AverageFrameRate, double? RawFrameRate) probe
    )
    {
        if (mediaType == MediaType.Video)
        {
            var hasLowBitrate = probe.BitrateKbps < MinimumVideoBitrateKbps;

            return hasLowBitrate;
        }

        return false;
    }

    private async Task<bool> ConvertVideoAsync(
        string sourceFilePath,
        string outputFilePath,
        CancellationToken cancellationToken
    )
    {
        var result = false;

        try
        {
            EnsureDirectoryExists(Path.GetDirectoryName(outputFilePath));

            var ext = Path.GetExtension(sourceFilePath);

            await FFMpegArguments
                .FromFileInput(sourceFilePath)
                .OutputToFile(
                    outputFilePath,
                    true,
                    options =>
                    {
                        if (ext == ".webm")
                        {
                            options
                                .WithVideoCodec("libvpx")
                                .WithAudioCodec("libvorbis")
                                .WithCustomArgument($"-b:v {MinimumVideoBitrateKbps}k")
                                .WithCustomArgument("-threads 2");
                        }
                        else
                        {
                            options
                                .WithVideoCodec("libx264")
                                .WithAudioCodec("libmp3lame")
                                .WithAudioBitrate(MinimumAudioBitrateKbps)
                                .WithConstantRateFactor(20)
                                .WithCustomArgument("-vf fps=30")
                                .WithCustomArgument("-pix_fmt yuv420p")
                                .WithCustomArgument("-preset veryfast")
                                .WithFastStart();
                        }
                    }
                )
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();

            if (File.Exists(outputFilePath))
            {
                File.SetLastWriteTimeUtc(outputFilePath, DateTime.Now);
                result = true;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ошибка конвертации видео {FilePath}", sourceFilePath);
        }

        return result;
    }

    private static void EnsureDirectoryExists(string? directoryPath)
    {
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }
}
