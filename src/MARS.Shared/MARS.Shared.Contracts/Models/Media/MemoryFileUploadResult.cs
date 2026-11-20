namespace MARS.Shared.Contracts.Models.Media;

/// <summary>
/// Результат загрузки файла во внутреннее memory-хранилище.
/// </summary>
public class MemoryFileUploadResult
{
    /// <summary>Путь, по которому оверлей загрузит файл (<c>/memory/{FileName}</c>).</summary>
    public string Path { get; set; } = string.Empty;
}
