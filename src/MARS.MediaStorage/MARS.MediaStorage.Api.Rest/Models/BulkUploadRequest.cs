using MARS.MediaStorage.Application.Media;
using MARS.MediaStorage.Application.Storage;
using MARS.Shared.Contracts.Models;
using Microsoft.AspNetCore.Mvc;

namespace MARS.MediaStorage.Api.Rest.Models;

public class BulkUploadRequest
{
    /// <summary>
    /// Каталог назначения относительно wwwroot. Необязателен: при пустом
    /// значении файл ложится в корень хранилища.
    /// </summary>
    public string TargetDirectory { get; set; } = string.Empty;
}
