namespace MARS.Shared.Grpc;

/// <summary>
/// Хранилище настройки раскладки ADHD-экрана для <c>TelegramusGrpcService</c>.
/// </summary>
/// <remarks>
/// Сам gRPC-сервис живёт в MARS.Shared и поднимается в двух процессах, но
/// таблица настройки принадлежит MARS.Alerts: его реализация читает AlertsDb,
/// а в MARS.OBS остаётся <see cref="UnavailableAdhdConfigStore"/>, который
/// отвечает FailedPrecondition. Настройка едет JSON-байтами — так же, как
/// остальные объектные поля контракта.
/// </remarks>
public interface IAdhdConfigStore
{
    /// <summary>Читает текущую настройку.</summary>
    Task<string> GetAsync(CancellationToken cancellationToken);

    /// <summary>Сохраняет настройку и возвращает то, что реально записано.</summary>
    Task<string> UpdateAsync(string configJson, CancellationToken cancellationToken);
}
