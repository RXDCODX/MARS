using MARS.Shared.Contracts.Models.Media;
using Microsoft.AspNetCore.Http;

namespace MARS.MediaStorage.Application.Alerts;

/// <summary>
/// Результат сохранения алерта. Ошибка валидации — это не исключение, а
/// ожидаемый исход, который контроллер возвращает клиенту как <c>Fail</c>.
/// </summary>
public sealed record AlertSaveResult(MediaInfo? Alert, string? Error)
{
    public bool Succeeded => Error is null;

    public static AlertSaveResult Ok(MediaInfo alert) => new(alert, null);

    public static AlertSaveResult Fail(string error) => new(null, error);
}
