using MARS.Shared.Contracts.Models.Media;

namespace MARS.Platform.Infrastructure.Clients.Http;

/// <summary>
/// Доступ к MARS.MediaStorage — единственному владельцу медиа-алертов.
/// TwitchCore хранит только <c>MediaInfoId</c> (в <c>ChannelRewardRecord</c>,
/// <c>HelloVideosUsers</c>), а сам <see cref="MediaInfo"/> получает отсюда.
/// </summary>
public interface IMediaStorageClient
{
    /// <summary>
    /// Возвращает <see cref="MediaInfo"/> по идентификатору либо <c>null</c>,
    /// если алерт не найден или сервис недоступен.
    /// </summary>
    Task<MediaInfo?> GetMediaInfoAsync(
        Guid mediaInfoId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Атомарно забирает следующий элемент очереди и возвращает подготовленное медиа.
    /// null означает, что очередь пуста или MediaStorage недоступен.
    /// </summary>
    Task<MediaInfo?> GetNextRandomMemeAsync(
        string folderPath,
        string? displayName,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Все алерты хранилища. null — сервис недоступен; пустой массив — алертов
    /// в базе нет. Различие важно вызывающей стороне: первое означает «показать
    /// нечего, но механизм жив», второе — «алертов ещё не завели».
    /// </summary>
    Task<IReadOnlyList<MediaInfo>?> GetAllAlertsAsync(
        CancellationToken cancellationToken = default
    );
}
