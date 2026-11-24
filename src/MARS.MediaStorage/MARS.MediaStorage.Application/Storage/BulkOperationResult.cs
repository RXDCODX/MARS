using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.MediaStorage.Domain.Entities;

namespace MARS.MediaStorage.Application.Storage;

/// <summary>
/// Итог операции над набором файлов.
/// </summary>
/// <param name="Requested">Сколько записей передали.</param>
/// <param name="Succeeded">Сколько обработано без ошибок.</param>
/// <param name="Failed">Сколько пропущено с ошибкой.</param>
/// <param name="Errors">Человекочитаемые причины пропуска.</param>
public readonly record struct BulkOperationResult(
    int Requested,
    int Succeeded,
    int Failed,
    IReadOnlyList<string> Errors
);
