namespace MARS.Videos365.Entities;

/// <summary>
/// Видео, уже опубликованное в Telegram-канале.
/// Множество «уже опубликовано» перенесено из монолита без изменений: по нему
/// источник отсекает видео, которые не нужно выкладывать повторно.
/// </summary>
public class Video365
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Идентификатор видео на сайте-источнике. Ключ дедупликации, поэтому
    /// уникален — в монолите индекса не было, и каждая проверка «выложено ли
    /// уже» читала таблицу целиком.
    /// </summary>
    public int SiteId { get; set; }

    public required string Title { get; set; }
    public required string PlayerUrl { get; set; }
    public required string DirectLinkUrl { get; set; }
    public required string Description { get; set; }

    /// <summary>
    /// Прямая ссылка на файл с подписанным токеном. Токен имеет срок действия,
    /// поэтому поле хранит историю, а не рабочую ссылку: повторно скачать по
    /// нему нельзя.
    /// </summary>
    public required string DownloadUrl { get; set; }

    /// <summary>
    /// Момент публикации. В монолите не заполнялось и лежало как
    /// <c>-infinity</c> (DateTime.MinValue в Npgsql), поэтому миграция приводит
    /// значение к null, а не к выдуманной дате.
    /// </summary>
    public DateTime? DateUpload { get; set; }

    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Идентификатор сообщения в Telegram. В монолите не заполнялся: наружу
    /// уходил <c>random_id</c> для идемпотентности, а не идентификатор
    /// сообщения.
    /// </summary>
    public long TelegramMessageId { get; set; }

    public bool IsUploaded { get; set; }
    public int VideoHeight { get; set; }
    public int VideoWidth { get; set; }
}
