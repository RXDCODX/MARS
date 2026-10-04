using System.Text;
using System.Text.RegularExpressions;

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
public partial class ComposeReadinessTests
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
        var block = ServiceBlock("postgres");
        var test = HealthcheckTest(block);

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
        var block = ServiceBlock("rabbitmq");
        var test = HealthcheckTest(block);

        Assert.True(
            test.Contains("check_port_connectivity"),
            "Healthcheck rabbitmq ограничен проверкой живости ноды и не проверяет,"
                + " что порт AMQP принимает соединения:"
                + Environment.NewLine
                + "  "
                + test
        );
    }

    /// <summary>
    /// Блок сервиса из <c>docker-compose.yml</c>.
    /// </summary>
    /// <remarks>
    /// Следующий сервис начинается строкой вида <c>  имя:</c> — две позиции
    /// отступа, слово, двоеточие. Вложенные ключи уходят глубже, а комментарии
    /// начинаются с <c>#</c> и ключами не являются: без этой оговорки блок
    /// сервиса наползал бы на следующий и проверка проходила бы не по тому тексту.
    /// </remarks>
    private static string ServiceBlock(string service)
    {
        var path = ClientUiImageWorkflowTests.FindRepositoryFile("docker-compose.yml");
        var lines = File.ReadAllText(path).Split('\n');

        var start = Array.FindIndex(lines, line => line.TrimEnd('\r') == $"  {service}:");

        Assert.True(start >= 0, $"В docker-compose.yml нет сервиса {service}.");

        var block = new List<string>();

        for (var index = start + 1; index < lines.Length; index++)
        {
            var line = lines[index].TrimEnd('\r');

            if (ServiceStart().IsMatch(line))
            {
                break;
            }

            block.Add(line);
        }

        return string.Join(Environment.NewLine, block);
    }

    /// <summary>Значение ключа <c>test</c> в healthcheck.</summary>
    /// <remarks>
    /// Значение — flow-последовательность, и разбивать его по строкам законно, так
    /// что чтение одной строки проверяло бы форматирование, а не смысл. Поэтому
    /// строки склеиваются, пока не раскроются скобки.
    /// </remarks>
    private static string HealthcheckTest(string block)
    {
        var lines = block.Split('\n');
        var start = Array.FindIndex(lines, line => TestKeyLine().Match(line).Success);

        Assert.True(
            start >= 0,
            "У сервиса нет healthcheck с ключом test, поэтому compose считает его"
                + " здоровым без всякой проверки."
        );

        var value = new StringBuilder(TestKeyLine().Match(lines[start]).Groups["value"].Value);
        var index = start;

        // Читаем, пока скобки не сойдутся. Условие в двух частях, а не одно
        // «пока не сойдутся»: у значения, разбитого по строкам, на первой строке
        // пусто, а пустое сбалансировано по скобкам, и цикл вышел бы, не
        // прочитав ничего.
        while (index + 1 < lines.Length && (Unclosed(value.ToString()) > 0 || IsBlank(value)))
        {
            index++;
            value.Append(' ').Append(lines[index].Trim());
        }

        return value.ToString().Trim();
    }

    private static bool IsBlank(StringBuilder value) => value.ToString().Trim().Length == 0;

    /// <summary>Сколько потоков ещё не закрыто: отрицательное — лишняя закрывающая.</summary>
    private static int Unclosed(string text)
    {
        var opened = 0;

        foreach (var character in text)
        {
            if (character == '[')
            {
                opened++;
            }
            else if (character == ']')
            {
                opened--;
            }
        }

        return opened;
    }

    [GeneratedRegex("^  [A-Za-z0-9_.-]+:\\s*$")]
    private static partial Regex ServiceStart();

    /// <summary>Ключ <c>test</c> с возможно пустым значением: поток может идти</summary>
    /// <remarks>
    /// следующими строками. Значение не может быть обязательным и не может
    /// захватывать <c>\r</c>: в файле переводы строк CRLF, и <c>.</c> в .NET
    /// совпадает и с <c>\r</c>, из-за чего значение читалось как одна
    /// переводная строка, то есть как пустое.
    /// </remarks>
    [GeneratedRegex("^\\s*test:\\s*(?<value>[^\\r\\n]*)")]
    private static partial Regex TestKeyLine();
}
