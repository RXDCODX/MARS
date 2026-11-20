namespace MARS.Shared.Contracts.Models.Media;

/// <summary>
/// Просьба положить файл во внутреннее memory-хранилище MARS.MediaStorage.
/// </summary>
/// <remarks>
/// Контракт внутреннего REST-вызова
/// <c>POST api/internal/media/memory-files</c>. Используется сервисами, которые
/// публикуют события оверлея с медиа (например, MARS.Telegram при приходе
/// сообщения в канале), но не владеют хранилищем.
/// </remarks>
public class MemoryFileUploadRequest
{
    /// <summary>Имя файла, под которым он будет доступен по <c>/memory/{FileName}</c>.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Содержимое файла в base64.</summary>
    public string ContentBase64 { get; set; } = string.Empty;
}
