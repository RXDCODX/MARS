namespace MARS.Shared.Contracts.Models.Overlay;

/// <summary>
/// Итог вброса события в оверлей.
/// </summary>
/// <remarks>
/// Контракт ответа внутреннего вызова <c>POST api/internal/overlay/events</c>.
/// Число подписчиков полезно вызывающему в журнале: ноль означает, что событие
/// принято, но показать его сейчас некому.
/// </remarks>
public class OverlayFireResult
{
    /// <summary>Сколько подписчиков оверлея получило событие.</summary>
    public int Subscribers { get; set; }
}
