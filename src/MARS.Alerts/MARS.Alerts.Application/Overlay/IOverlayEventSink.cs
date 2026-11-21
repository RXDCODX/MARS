using MARS.Shared.Contracts;

namespace MARS.Alerts.Application.Overlay;

/// <summary>
/// Порт доставки событий оверлея в хаб. Реализация живёт в Presentation.Overlay
/// и использует типизированный <c>IHubContext</c>. Заменяет удалённый
/// <c>EventBroadcaster&lt;TelegramusEvent&gt;</c> для слоя Application, который
/// не может напрямую ссылаться на Presentation.Overlay (иначе цикл).
/// </summary>
public interface IOverlayEventSink
{
    Task BroadcastAsync(
        TelegramusEvent notification,
        CancellationToken cancellationToken = default
    );
}
