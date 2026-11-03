namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Закрывает процессы, которые награда «Выключить игру» должна погасить.
/// </summary>
/// <remarks>
/// Отдельный интерфейс, а не прямой <c>Process.Kill</c> в эффекте: тест не
/// может убивать процессы на машине разработчика, а без подмены проверялась бы
/// не логика награды, а наличие игры. Список имён — часть контракта эффекта,
/// поэтому он остался в реализации.
/// </remarks>
public interface IGameCloser
{
    Task CloseAsync(CancellationToken cancellationToken = default);
}
