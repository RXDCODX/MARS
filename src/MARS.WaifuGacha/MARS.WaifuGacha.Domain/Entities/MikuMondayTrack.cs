using System.ComponentModel.DataAnnotations;

namespace MARS.WaifuGacha.Domain.Entities;

/// <summary>
/// Связующая таблица между наградой Miku Monday и BaseTrackInfo
/// Хранит только номер трека в плейлисте Miku
/// </summary>
public class MikuMondayTrack
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Номер трека в списке Miku (1-27)
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// ID трека в BaseTrackInfo
    /// </summary>
    public Guid BaseTrackInfoId { get; set; }

    /// <summary>
    /// Название трека. Дублируется из BaseTrackInfo, чтобы MikuMonday
    /// не зависел от сервиса SoundRequest.
    /// </summary>
    public string TrackName { get; set; } = string.Empty;

    /// <summary>
    /// Исполнитель (авторы из BaseTrackInfo через запятую).
    /// </summary>
    public string Artist { get; set; } = string.Empty;

    /// <summary>
    /// Ссылка на трек на YouTube.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Обложка трека.
    /// </summary>
    public string? ArtworkUrl { get; set; }

    /// <summary>
    /// Дата создания записи
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
