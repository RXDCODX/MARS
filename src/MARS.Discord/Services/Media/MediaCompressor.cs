using MARS.Shared.Models;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace MARS.Discord.Services.Media;

/// <summary>
/// Уменьшение вложений до лимита Discord.
/// </summary>
/// <remarks>
/// Изображения перекодируются по лестнице качества JPEG, видео сначала
/// сжимается, а потом режется на части, аудио только сжимается. Ключевое
/// отличие от монолита: вместо исключений возвращается
/// <see cref="OperationResult{T}"/>, потому что «не влезло даже после сжатия» —
/// это нормальный исход, а не сбой сервиса.
/// </remarks>
public sealed class MediaCompressor(IFfmpegRunner ffmpegRunner, ILogger<MediaCompressor> logger)
    : IMediaCompressor
{
    private const int MaxImageDimension = 1920;

    /// <summary>Доля от лимита, на которую нарезается видео.</summary>
    private const double SegmentSizeShare = 0.85;

    /// <summary>Минимальная длина фрагмента, секунды.</summary>
    private const int MinimumSegmentSeconds = 10;

    private static readonly int[] JpegQualityLadder = [85, 75, 60, 45];

    private const string VideoArguments =
        "-i {input} -c:v libx264 -crf 28 -preset veryfast "
        + "-vf scale='min(1280,iw)':min(720,ih):force_original_aspect_ratio=decrease "
        + "-r 30 -c:a aac -b:a 128k -movflags +faststart -y {output}";

    private const string AudioArguments = "-i {input} -c:a libmp3lame -b:a 96k -ac 1 -y {output}";

    public async Task<OperationResult<Stream>> CompressImageAsync(
        Stream source,
        string fileName,
        long maxSize,
        CancellationToken cancellationToken = default
    )
    {
        var result = OperationResult<Stream>.Fail("Стартовая ошибка сжатия изображения");

        try
        {
            // Буфер не уничтожается по using: если файл уже укладывается в
            // лимит, его поток уходит в результат, и using закрыл бы его
            // ещё до возврата вызывающей стороне.
            var buffered = await BufferAsync(source, cancellationToken);

            if (buffered.Length <= maxSize)
            {
                result = OperationResult<Stream>.Ok(buffered);
            }
            else
            {
                using (buffered)
                {
                    result = await ShrinkAsync(buffered, fileName, maxSize, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось сжать изображение {FileName}", fileName);
            result = OperationResult<Stream>.Fail($"Не удалось сжать изображение: {ex.Message}");
        }

        return result;
    }

    public async Task<OperationResult<IReadOnlyList<CompressedVideoSegment>>> CompressVideoAsync(
        Stream source,
        string fileName,
        long maxSize,
        CancellationToken cancellationToken = default
    )
    {
        var result = OperationResult<IReadOnlyList<CompressedVideoSegment>>.Fail(
            "Стартовая ошибка сжатия видео"
        );

        var tempDirectory = CreateTempDirectory("mars_video");

        try
        {
            var inputPath = await WriteTempAsync(
                tempDirectory,
                $"input{Path.GetExtension(fileName)}",
                source,
                cancellationToken
            );
            var compressedPath = Path.Combine(tempDirectory, "compressed.mp4");

            var encoded = await ffmpegRunner.RunAsync(
                BuildArguments(VideoArguments, inputPath, compressedPath),
                cancellationToken
            );

            if (!encoded)
            {
                logger.LogInformation("Не удалось сжать видео {FileName}", fileName);
                result = OperationResult<IReadOnlyList<CompressedVideoSegment>>.Fail(
                    "Не удалось сжать видео"
                );
            }
            else
            {
                result = await PublishAsync(
                    compressedPath,
                    fileName,
                    maxSize,
                    tempDirectory,
                    cancellationToken
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось обработать видео {FileName}", fileName);
            result = OperationResult<IReadOnlyList<CompressedVideoSegment>>.Fail(
                $"Не удалось обработать видео: {ex.Message}"
            );
        }
        finally
        {
            DeleteDirectory(tempDirectory);
        }

        return result;
    }

    public async Task<OperationResult<Stream>> CompressAudioAsync(
        Stream source,
        string fileName,
        long maxSize,
        CancellationToken cancellationToken = default
    )
    {
        var result = OperationResult<Stream>.Fail("Стартовая ошибка сжатия аудио");

        var tempDirectory = CreateTempDirectory("mars_audio");

        try
        {
            var inputPath = await WriteTempAsync(
                tempDirectory,
                $"input{Path.GetExtension(fileName)}",
                source,
                cancellationToken
            );
            var outputPath = Path.Combine(tempDirectory, "output.mp3");

            var encoded = await ffmpegRunner.RunAsync(
                BuildArguments(AudioArguments, inputPath, outputPath),
                cancellationToken
            );

            if (!encoded)
            {
                logger.LogInformation("Не удалось сжать аудио {FileName}", fileName);
                result = OperationResult<Stream>.Fail("Не удалось сжать аудио");
            }
            else
            {
                var size = new FileInfo(outputPath).Length;

                if (size > maxSize)
                {
                    logger.LogInformation(
                        "Аудио {FileName} не удалось сжать до лимита {MaxSize} байт (получено {Size})",
                        fileName,
                        maxSize,
                        size
                    );
                    result = OperationResult<Stream>.Fail(
                        "Аудио не помещается в лимит даже после сжатия"
                    );
                }
                else
                {
                    result = OperationResult<Stream>.Ok(
                        new MemoryStream(
                            await File.ReadAllBytesAsync(outputPath, cancellationToken)
                        )
                    );
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось обработать аудио {FileName}", fileName);
            result = OperationResult<Stream>.Fail($"Не удалось обработать аудио: {ex.Message}");
        }
        finally
        {
            DeleteDirectory(tempDirectory);
        }

        return result;
    }

    private async Task<OperationResult<IReadOnlyList<CompressedVideoSegment>>> PublishAsync(
        string compressedPath,
        string fileName,
        long maxSize,
        string tempDirectory,
        CancellationToken cancellationToken
    )
    {
        var result = OperationResult<IReadOnlyList<CompressedVideoSegment>>.Fail(
            "Стартовая ошибка публикации видео"
        );

        var compressedSize = new FileInfo(compressedPath).Length;

        if (compressedSize <= maxSize)
        {
            result = OperationResult<IReadOnlyList<CompressedVideoSegment>>.Ok([
                new CompressedVideoSegment(
                    new MemoryStream(
                        await File.ReadAllBytesAsync(compressedPath, cancellationToken)
                    ),
                    fileName
                ),
            ]);
        }
        else
        {
            result = await SplitAsync(
                compressedPath,
                fileName,
                compressedSize,
                maxSize,
                tempDirectory,
                cancellationToken
            );
        }

        return result;
    }

    private async Task<OperationResult<IReadOnlyList<CompressedVideoSegment>>> SplitAsync(
        string compressedPath,
        string fileName,
        long compressedSize,
        long maxSize,
        string tempDirectory,
        CancellationToken cancellationToken
    )
    {
        var result = OperationResult<IReadOnlyList<CompressedVideoSegment>>.Fail(
            "Стартовая ошибка нарезки видео"
        );

        var duration = await ffmpegRunner.GetDurationAsync(compressedPath, cancellationToken);

        var segmentCount = (int)Math.Ceiling((double)compressedSize / (maxSize * SegmentSizeShare));
        var segmentTime = (int)(duration.TotalSeconds / segmentCount);

        // Короче десяти секунд фрагмент бесполезен: зритель не успевает ни
        // посмотреть, ни понять, что произошло.
        segmentTime = Math.Max(segmentTime, MinimumSegmentSeconds);

        var segmentsDirectory = Path.Combine(tempDirectory, "segments");
        Directory.CreateDirectory(segmentsDirectory);
        var pattern = Path.Combine(segmentsDirectory, "segment_%03d.mp4");

        var split = await ffmpegRunner.RunAsync(
            $"-i {compressedPath} -c copy -f segment -segment_time {segmentTime} "
                + $"-reset_timestamps 1 -y {pattern}",
            cancellationToken
        );

        if (!split)
        {
            logger.LogInformation("Не удалось разрезать видео {FileName}", fileName);
            result = OperationResult<IReadOnlyList<CompressedVideoSegment>>.Fail(
                "Не удалось разрезать видео на фрагменты"
            );
        }
        else
        {
            result = await ReadSegmentsAsync(
                segmentsDirectory,
                fileName,
                maxSize,
                cancellationToken
            );
        }

        return result;
    }

    private async Task<OperationResult<IReadOnlyList<CompressedVideoSegment>>> ReadSegmentsAsync(
        string segmentsDirectory,
        string fileName,
        long maxSize,
        CancellationToken cancellationToken
    )
    {
        var result = OperationResult<IReadOnlyList<CompressedVideoSegment>>.Fail(
            "Стартовая ошибка чтения фрагментов"
        );

        var files = Directory
            .GetFiles(segmentsDirectory, "segment_*.mp4")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (files.Length == 0)
        {
            result = OperationResult<IReadOnlyList<CompressedVideoSegment>>.Fail(
                "Нарезка не дала ни одного фрагмента"
            );
        }
        else
        {
            var oversized = Array.Find(files, path => new FileInfo(path).Length > maxSize);

            if (oversized is not null)
            {
                logger.LogInformation(
                    "Фрагмент {FileName} не помещается в лимит даже после нарезки",
                    fileName
                );
                result = OperationResult<IReadOnlyList<CompressedVideoSegment>>.Fail(
                    "Фрагмент видео не помещается в лимит даже после нарезки"
                );
            }
            else
            {
                var segments = new List<CompressedVideoSegment>(files.Length);

                foreach (var path in files)
                {
                    segments.Add(
                        new CompressedVideoSegment(
                            new MemoryStream(await File.ReadAllBytesAsync(path, cancellationToken)),
                            Path.GetFileName(path)
                        )
                    );
                }

                logger.LogInformation(
                    "Видео {FileName} разрезано на {Count} фрагментов",
                    fileName,
                    segments.Count
                );

                result = OperationResult<IReadOnlyList<CompressedVideoSegment>>.Ok(segments);
            }
        }

        return result;
    }

    private async Task<OperationResult<Stream>> ShrinkAsync(
        MemoryStream buffered,
        string fileName,
        long maxSize,
        CancellationToken cancellationToken
    )
    {
        var result = OperationResult<Stream>.Fail("Стартовая ошибка перекодирования");

        try
        {
            using var image = await Image.LoadAsync(buffered, cancellationToken);

            if (image.Width > MaxImageDimension || image.Height > MaxImageDimension)
            {
                image.Mutate(context =>
                    context.Resize(
                        new ResizeOptions
                        {
                            Size = new Size(MaxImageDimension, MaxImageDimension),
                            Mode = ResizeMode.Max,
                        }
                    )
                );
            }

            foreach (var quality in JpegQualityLadder)
            {
                var output = new MemoryStream();
                var encoder = new JpegEncoder { Quality = quality };
                await image.SaveAsync(output, encoder, cancellationToken);

                if (output.Length <= maxSize)
                {
                    output.Position = 0;
                    logger.LogInformation(
                        "Изображение {FileName} сжато до {Size} байт (JPEG quality={Quality})",
                        fileName,
                        output.Length,
                        quality
                    );
                    result = OperationResult<Stream>.Ok(output);
                    break;
                }

                await output.DisposeAsync();
            }

            if (!result.Success)
            {
                logger.LogInformation(
                    "Изображение {FileName} не удалось сжать до лимита {MaxSize} байт",
                    fileName,
                    maxSize
                );
                result = OperationResult<Stream>.Fail(
                    "Изображение не помещается в лимит даже после сжатия"
                );
            }
        }
        catch (UnknownImageFormatException)
        {
            logger.LogInformation("Файл {FileName} не является изображением", fileName);
            result = OperationResult<Stream>.Fail("Файл не является изображением");
        }

        return result;
    }

    private static string BuildArguments(string template, string input, string output) =>
        template.Replace("{input}", input).Replace("{output}", output);

    private static async Task<MemoryStream> BufferAsync(
        Stream source,
        CancellationToken cancellationToken
    )
    {
        var buffered = new MemoryStream();
        await source.CopyToAsync(buffered, cancellationToken);
        buffered.Position = 0;

        return buffered;
    }

    private static async Task<string> WriteTempAsync(
        string directory,
        string fileName,
        Stream source,
        CancellationToken cancellationToken
    )
    {
        var path = Path.Combine(directory, fileName);
        await using var file = File.Create(path);
        await source.CopyToAsync(file, cancellationToken);

        return path;
    }

    private static string CreateTempDirectory(string prefix)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        return directory;
    }

    private void DeleteDirectory(string directory)
    {
        try
        {
            Directory.Delete(directory, true);
        }
        catch (IOException)
        {
            logger.LogWarning("Не удалось удалить временный каталог {Directory}", directory);
        }
        catch (UnauthorizedAccessException)
        {
            logger.LogWarning("Нет прав на удаление временного каталога {Directory}", directory);
        }
    }
}
