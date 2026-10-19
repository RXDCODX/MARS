using System.Net.Sockets;

namespace MARS.Shared.Development;

/// <summary>
/// Проба адреса TCP-подключением: ровно то, что сервис сделает дальше, когда
/// пойдёт в базу или брокер, поэтому ответ совпадает с тем, что сервис увидит.
/// </summary>
/// <remarks>
/// Ожидание синхронное и короткое: это шаг запуска, а не рабочий путь. Локальный
/// закрытый порт отвечает отказом сразу; таймаут страхует от адреса, который
/// молча дропает пакеты и держит подключение минутами.
/// </remarks>
public sealed class TcpEndpointProbe : IDevEndpointProbe
{
    /// <summary>Предельное ожидание ответа на одну пробу.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(500);

    /// <inheritdoc />
    public DevEndpointState Probe(string host, int port)
    {
        var state = DevEndpointState.Unknown;

        if (!string.IsNullOrWhiteSpace(host) && port > 0)
        {
            state = TryConnect(host, port);
        }

        return state;
    }

    /// <summary>Подключение к адресу: «не отвечает» вместо исключения.</summary>
    private static DevEndpointState TryConnect(string host, int port)
    {
        var state = DevEndpointState.NotListening;

        try
        {
            using var client = new TcpClient();

            // ConnectAsync(...).Wait(timeout), а не синхронный Connect: у
            // синхронного нет таймаута, и молчащий адрес вешал бы старт сервиса.
            var connected = client.ConnectAsync(host, port).Wait(Timeout);

            if (connected && client.Connected)
            {
                state = DevEndpointState.Listening;
            }
        }
        catch (Exception)
        {
            // Отказ соединения, неразрешимое имя, отсутствие сети — всё это
            // «не отвечает». Исключение наружу не идёт: старт сервиса не должен
            // зависеть от того, что проба не удалась.
            state = DevEndpointState.NotListening;
        }

        return state;
    }
}
