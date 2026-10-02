using MARS.Shared.Models;

namespace MARS.Discord.Services.Media;

/// <summary>
/// Часть видео, получившаяся после нарезки слишком большого файла.
/// </summary>
public sealed record CompressedVideoSegment(Stream Stream, string FileName);

/// <summary>
/// Уменьшение вложений до лимита Discord.
/// </summary>
public interface IMediaCompressor
{
    /// <summary>
    /// Сжимает изображение. Возвращает поток, если файл уже укладывается в
    /// лимит или стал меньше после перекодирования; иначе ошибку с причиной.
    /// </summary>
    Task<OperationResult<Stream>> CompressImageAsync(
        Stream source,
        string fileName,
        long maxSize,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Сжимает видео, а если результат всё ещё больше лимита — режет его на
    /// части. Ошибка означает, что отправить нечего: ffmpeg не отработал либо
    /// отдельный фрагмент не влез в лимит.
    /// </summary>
    Task<OperationResult<IReadOnlyList<CompressedVideoSegment>>> CompressVideoAsync(
        Stream source,
        string fileName,
        long maxSize,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Сжимает аудио. Аудио не режется: если после сжатия файл не влез,
    /// отправлять нечего.
    /// </summary>
    Task<OperationResult<Stream>> CompressAudioAsync(
        Stream source,
        string fileName,
        long maxSize,
        CancellationToken cancellationToken = default
    );
}
