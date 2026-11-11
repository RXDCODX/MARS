using MARS.MediaStorage.Application.Media;
using MARS.MediaStorage.Application.Storage;
using MARS.Shared.Contracts.Models;
using Microsoft.AspNetCore.Mvc;

namespace MARS.MediaStorage.Api.Rest.Models;

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
