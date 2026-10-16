namespace MARS.Shared.Development;

/// <summary>
/// Проба одного адреса инфраструктуры.
/// </summary>
/// <remarks>
/// Интерфейс, а не статический метод: тесты подставляют ответ, не занимая
/// настоящий порт, а стартер не зависит от сети.
/// </remarks>
public interface IDevEndpointProbe
{
    /// <summary>Отвечает ли адрес.</summary>
    /// <param name="host">Хост из конфигурации сервиса.</param>
    /// <param name="port">Порт на том же хосте.</param>
    DevEndpointState Probe(string host, int port);
}
