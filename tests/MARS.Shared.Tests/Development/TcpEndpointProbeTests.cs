using System.Net;
using System.Net.Sockets;
using MARS.Shared.Development;

namespace MARS.Shared.Tests.Development;

/// <summary>
/// Проба адреса инфраструктуры настоящим TCP-подключением, а не заглушкой: у
/// заглушки проверялось бы только то, что она возвращает то, что в неё положили.
/// От пробы нужно ровно то, что увидит сам сервис, когда пойдёт в базу.
/// </summary>
public class TcpEndpointProbeTests
{
    [Fact]
    public void Открытый_порт_отвечает()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;

            Assert.Equal(
                DevEndpointState.Listening,
                new TcpEndpointProbe().Probe("127.0.0.1", port)
            );
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public void Закрытый_порт_не_отвечает()
    {
        var port = ClosedPort();

        Assert.Equal(
            DevEndpointState.NotListening,
            new TcpEndpointProbe().Probe("127.0.0.1", port)
        );
    }

    [Fact]
    public void Неразрешимое_имя_не_отвечает()
    {
        // Зона .invalid зарезервирована и не разрешается никогда, а ожидание
        // ограничено: проба обязана вернуться, а не держать старт сервиса.
        var state = new TcpEndpointProbe().Probe("mars-нет-такого-хоста.invalid", 6017);

        Assert.Equal(DevEndpointState.NotListening, state);
    }

    /// <summary>
    /// «Нечем проверить» — отдельный ответ, а не «не отвечает»: пустой адрес и
    /// нулевой порт означают, что строку подключения не разобрали, и по этому
    /// ответу автостарт поднимает службу, а не пропускает её.
    /// </summary>
    [Fact]
    public void Адрес_без_координат_проверить_нечем()
    {
        var probe = new TcpEndpointProbe();

        Assert.Equal(DevEndpointState.Unknown, probe.Probe(string.Empty, 6017));
        Assert.Equal(DevEndpointState.Unknown, probe.Probe("localhost", 0));
        Assert.Equal(DevEndpointState.Unknown, probe.Probe("localhost", -1));
    }

    /// <summary>Свободный порт: слушатель занимает порт, узнаёт его и отпускает.</summary>
    private static int ClosedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return port;
    }
}
