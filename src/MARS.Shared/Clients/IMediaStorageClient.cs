using MARS.Shared.Models.Media;

namespace MARS.Shared.Clients;

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
}
