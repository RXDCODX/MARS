namespace MARS.MediaStorage.Services.Storage;

/// <summary>
/// Загружаемый файл. Отвязан от <c>IFormFile</c>, чтобы сервис не зависел от
/// ASP.NET и проверялся обычными юнит-тестами.
/// </summary>
/// <param name="FileName">Имя файла от клиента — недоверенные данные.</param>
/// <param name="Content">Поток содержимого. Считывается до конца и закрывается.</param>
/// <param name="ContentType">MIME-тип, если клиент его прислал.</param>
/// <param name="Length">Размер, если известен заранее.</param>
public sealed record MediaUploadFile(
    string FileName,
    Stream Content,
    string? ContentType = null,
    long? Length = null
) : IDisposable
{
    public void Dispose() => Content.Dispose();
}
