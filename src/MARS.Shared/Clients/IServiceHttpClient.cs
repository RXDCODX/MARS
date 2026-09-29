namespace MARS.Shared.Clients;

/// <summary>
/// Базовый клиент межсервисного HTTP-общения. Реализует API-key авторизацию
/// и разбор общего конверта <see cref="Models.OperationResult{T}"/>.
/// Наследники реализуют только <see cref="ServiceEndpoint"/> и <see cref="GetAsync{T}"/>.
/// </summary>
public interface IServiceHttpClient
{
    /// <summary>Базовый адрес сервиса-владельца (например, <c>http://waifu-gacha:8080</c>).</summary>
    string ServiceEndpoint { get; }

    /// <summary>
    /// Выполняет GET-запрос и достаёт <c>data</c> из конверта
    /// <see cref="Models.OperationResult{T}"/>. Возвращает <c>null</c>, если сервис
    /// недоступен, ответил ошибкой или <c>IsSuccess == false</c>.
    /// </summary>
    Task<T?> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполняет POST-запрос с JSON-телом и достаёт <c>data</c> из конверта
    /// <see cref="Models.OperationResult{T}"/>. Возвращает <c>null</c> при тех же
    /// условиях, что и <see cref="GetAsync{T}"/>.
    /// </summary>
    Task<T?> PostAsync<TRequest, T>(
        string relativeUrl,
        TRequest body,
        CancellationToken cancellationToken = default
    );
}
