using MARS.Shared.Models.Media;

namespace MARS.Alerts.Services.Alerts;

/// <summary>
/// Источник алертов с ключевыми словами.
/// </summary>
/// <remarks>
/// Отдельный интерфейс, а не прямой вызов MARS.MediaStorage: таблица
/// <c>Alerts</c> принадлежит хранилищу, а читает её MARS.Alerts. Проверка
/// идёт через HTTP-клиент, и без интерфейса потребитель нельзя было бы
/// проверить без поднятого хранилища.
/// </remarks>
public interface IEnabledAlertSource
{
    /// <summary>
    /// Включённые алерты, у которых задан ключевой триггер. null означает, что
    /// источник недоступен: показывать при этом нечего, и подменять пустым
    /// списком значило бы тихо отключить механику.
    /// </summary>
    Task<IReadOnlyList<MediaInfo>?> GetEnabledAlertsAsync(
        CancellationToken cancellationToken = default
    );
}
