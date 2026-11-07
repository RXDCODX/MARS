using MARS.MediaStorage.Entities.DTOs;
using MARS.MediaStorage.Services.Media;
using MARS.MediaStorage.Services.Storage;
using MARS.Shared.Contracts.Models;
using Microsoft.AspNetCore.Mvc;

namespace MARS.MediaStorage.Controllers;

public class BulkStorageRequest
{
    /// <summary>
    /// Идентификаторы записей хранилища. Пустой список — ошибка, а не «ничего».
    /// </summary>
    public required List<Guid> Ids { get; set; }

    /// <summary>
    /// Пробный прогон: операция не применяется, но считается и возвращает
    /// результат, который дала бы. Нужен, чтобы пользователь увидел, что
    /// произойдёт, до необратимого действия.
    /// </summary>
    public bool DryRun { get; set; }
}
