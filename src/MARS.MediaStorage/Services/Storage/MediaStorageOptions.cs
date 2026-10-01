namespace MARS.MediaStorage.Services.Storage;

/// <summary>
/// Настройки файлового хранилища.
/// </summary>
public sealed class MediaStorageOptions
{
    public const string SectionName = "MediaStorage";

    /// <summary>
    /// Сколько дней удалённый файл лежит в корзине до безвозвратного удаления.
    /// </summary>
    public int TrashRetentionDays { get; set; } = 30;

    /// <summary>
    /// Включать ли фоновую очистку корзины. По умолчанию включено: иначе
    /// корзина росла бы бесконечно.
    /// </summary>
    public bool EnablePurgeWorker { get; set; } = true;

    /// <summary>
    /// Как часто проверять корзину. По умолчанию раз в сутки.
    /// </summary>
    public int PurgeIntervalMinutes { get; set; } = 60 * 24;

    /// <summary>
    /// Максимальный размер одного загружаемого файла, МБ.
    /// </summary>
    /// <remarks>
    /// По умолчанию 95 МБ — чуть ниже жёсткого лимита GitHub в 100 МБ. Файл
    /// больше лимита нельзя закоммитить и запушить, поэтому принимать его в
    /// хранилище бессмысленно: он лёг бы на диск, а push падал бы навсегда.
    /// </remarks>
    public int MaxUploadSizeMb { get; set; } = 95;

    public long MaxUploadBytes => (long)MaxUploadSizeMb * 1024 * 1024;

    public TimeSpan TrashRetention => TimeSpan.FromDays(TrashRetentionDays);
}
