using MARS.Shared.Contracts.Models.Media;

namespace MARS.MediaStorage.Application.RandomMemes;

/// <summary>
/// Сценарий выдачи следующего элемента очереди с подготовкой медиа для оверлея.
/// </summary>
public interface IRandomMemePlaybackService
{
    Task<MediaInfo?> GetNextPreparedMediaAsync(
        string folderPath,
        string? displayName,
        CancellationToken cancellationToken = default
    );
}
