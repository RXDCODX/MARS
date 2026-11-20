namespace MARS.Telegram.Application.Services;

/// <summary>
/// Загрузка файла во внутреннее memory-хранилище MARS.MediaStorage.
/// </summary>
public interface IMemoryMediaUploader
{
    Task<string?> UploadAsync(
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default
    );
}
