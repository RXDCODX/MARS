using MARS.MediaStorage.Entities.DTOs;
using MARS.MediaStorage.Services.Media;
using MARS.MediaStorage.Services.Storage;
using MARS.Shared.Contracts.Models;
using Microsoft.AspNetCore.Mvc;

namespace MARS.MediaStorage.Controllers;

public class BulkUploadRequest
{
    /// <summary>
    /// Каталог назначения относительно wwwroot. Необязателен: при пустом
    /// значении файл ложится в корень хранилища.
    /// </summary>
    public string TargetDirectory { get; set; } = string.Empty;
}
