namespace MARS.Telegram.Application.Services;

/// <summary>
/// Публикация события оверлея в сервис-владелец (MARS.Alerts) из другого процесса.
/// </summary>
public interface ITelegramusEventPublisher
{
    Task PublishAsync(string eventJson, CancellationToken cancellationToken = default);
}
