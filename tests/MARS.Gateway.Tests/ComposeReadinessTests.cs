namespace MARS.Gateway.Tests;

/// <summary>
/// Готовность инфраструктуры на холодном томе.
/// </summary>
/// <remarks>
/// Проверка ловит гонку, из-за которой стенд не поднимался на пустых томах, а в
/// CI — на каждом прогоне, потому что там тома всегда свежие.
/// <para>
/// Официальный entrypoint postgres на пустом томе запускает временный сервер
/// только на unix-сокете, выполняет <c>docker-entrypoint-initdb.d</c> и гасит его.
/// <c>pg_isready</c> без <c>-h</c> стучится именно в unix-сокет, поэтому
/// healthcheck становится зелёным в тот момент, когда TCP-порт ещё закрыт. На
/// этой машине разрыв составил 109 секунд: <c>ready to accept connections</c> в
/// 21:41:05, <c>listening on IPv4 0.0.0.0:5432</c> — только в 21:42:54.
/// </para>
/// <para>
/// Все зависимые сервисы compose запускает по <c>service_healthy</c>, то есть в
/// это окно, и первый же получает <c>Connection refused</c> на 5432. Дальше
/// срабатывает <c>restart: unless-stopped</c>, сервис поднимается со второй
/// попытки, и стенд в итоге здоров, но <c>--wait</c> к этому моменту уже отдался
/// «dependency failed to start: container … is unhealthy». Стек чинил себя сам,
/// а проверка падала вхолостую.
/// </para>
/// </remarks>
public class ComposeReadinessTests
{
    /// <summary>
    /// Готовность postgres обязана проверяться по TCP.
    /// </summary>
    /// <remarks>
    /// По сокету проверка зеленеет на временном сервере, то есть до создания баз
    /// сервисов из <c>01-databases.sh</c>. По TCP она зеленеет только там, где
    /// сервисы действительно могут подключиться.
    /// </remarks>
    [Fact]
    public void PostgresПроверяетсяПоTcp()
    {
        var block = ComposeFile.Block("postgres");
        var test = ComposeFile.HealthcheckTest(block);

        Assert.True(
            test.Contains("-h 127.0.0.1") || test.Contains("--host=127.0.0.1"),
            "Healthcheck postgres проверяет готовность без TCP-хоста, а значит по"
                + " unix-сокету. На пустом томе временный сервер entrypoint'а даёт"
                + " сокет раньше, чем порт 5432 начинает принимать, и compose"
                + " запускает сервисы заведомо неготовой базы:"
                + Environment.NewLine
                + "  "
                + test
        );
    }

    /// <summary>
    /// Готовность rabbitmq обязана включать проверку порта AMQP.
    /// </summary>
    /// <remarks>
    /// <c>rabbitmq-diagnostics -q ping</c> отвечает про живость ноды, а не про
    /// принимающий соединения порт 5672. Расхождение это короткое, но ровно его
    /// успевает застать сервис, которому нужен брокер: тот падает на подключении
    /// и полагается на рестарт.
    /// </remarks>
    [Fact]
    public void RabbitmqПроверяетПортAmqp()
    {
        var block = ComposeFile.Block("rabbitmq");
        var test = ComposeFile.HealthcheckTest(block);

        Assert.True(
            test.Contains("check_port_connectivity"),
            "Healthcheck rabbitmq ограничен проверкой живости ноды и не проверяет,"
                + " что порт AMQP принимает соединения:"
                + Environment.NewLine
                + "  "
                + test
        );
    }
}
