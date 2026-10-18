using System.Globalization;
using System.Text.RegularExpressions;

namespace MARS.Gateway.Tests;

/// <summary>
/// Схема хостовых портов стенда: базовый порт среды плюс порядковый номер.
/// </summary>
/// <remarks>
/// <para>
/// Хостовые порты задаёт одна переменная на среду — <c>MARS_PORT_BASE</c>, а
/// конкретные значения лежат в явных <c>MARS_PORT_&lt;СЕРВИС&gt;</c>, и каждое
/// обязано быть равно <c>base + номер</c>. Арифметики compose не умеет, поэтому
/// «base + номер» в самом файле не выразить; зато это выражается тестом — и
/// тогда сдвинуть весь стенд значит поменять одну строку в файле окружения, а не
/// тридцать портов в двух файлах.
/// </para>
/// <para>
/// Порядок сервисов и их контейнерные порты объявлены здесь, а не в шаблоне:
/// шаблон — это данные стенда, а порядок — договорённость, и вторая его копия в
/// шаблоне разошлась бы с первой при первом же новом сервисе. Явные переменные
/// без такого теста разъезжаются тихо: сдвинули базу — часть портов осталась на
/// старых местах, и стенд поднялся наполовину.
/// </para>
/// <para>
/// Две среды берут разные базы (dev 6001, prod 7001): два стенда обязаны
/// подниматься на одной машине одновременно, иначе разработка и боевая проверка
/// мешали бы друг другу занятыми портами.
/// </para>
/// <para>
/// Файлы читаются через <see cref="ClientUiImageWorkflowTests.FindRepositoryFile"/>:
/// копировать их в выход теста не нужно, а путь вглубь от <c>bin/Release</c>
/// рассыпался бы при смене конфигурации сборки.
/// </para>
/// </remarks>
public sealed partial class PortAllocationTests
{
    /// <summary>
    /// Порядок от базы: сначала приложения, затем инфраструктура.
    /// </summary>
    /// <remarks>
    /// Приложения идут в том порядке, в котором о них говорит <c>src/README.md</c>,
    /// инфраструктура — в том, в котором её объявляет <c>docker-compose.yml</c>.
    /// Контейнерный порт хранится рядом: проверка боевого стенда сверяет с ним
    /// публикацию, иначе опечатка вида <c>6024:3001</c> прошла бы молча.
    /// <para>
    /// Последние семь записей — вторые хостовые порты сервисов с gRPC и matoi.
    /// Они дописаны в конец, а не вставлены рядом со своим приложением: номер
    /// записи и есть порт, и вставка в середину сдвинула бы порты уже
    /// работающего стенда. Границы сервисов по этой причине идут не подряд.
    /// </para>
    /// </remarks>
    private static readonly (string Key, int Index, int ContainerPort)[] Scheme =
    [
        ("GATEWAY", 0, 8080),
        ("TWITCH_CORE", 1, 8080),
        ("WAIFU_GACHA", 2, 8080),
        ("TELEGRAM", 3, 8080),
        ("DISCORD", 4, 8080),
        ("COMMANDS", 5, 8080),
        ("SOUND_REQUEST", 6, 8080),
        ("TTS", 7, 8080),
        ("OBS", 8, 8080),
        ("ALERTS", 9, 8080),
        ("SCOREBOARD", 10, 8080),
        ("CINEMA_QUEUE", 11, 8080),
        ("MEDIA_STORAGE", 12, 8080),
        ("ADMIN", 13, 8080),
        ("VIDEOS365", 14, 8080),
        ("SHIKIMORI", 15, 8080),
        ("POSTGRES", 16, 5432),
        ("RABBITMQ", 17, 5672),
        ("RABBITMQ_MANAGEMENT", 18, 15672),
        ("TEMPO_HTTP", 19, 3200),
        ("TEMPO_OTLP_GRPC", 20, 4317),
        ("TEMPO_OTLP_HTTP", 21, 4318),
        ("PROMETHEUS", 22, 9090),
        ("GRAFANA", 23, 3000),
        ("LOKI", 24, 3100),
        ("ALLOY", 25, 12345),
        ("MATOI", 26, 3000),
        ("ALERTS_GRPC", 27, 8081),
        ("OBS_GRPC", 28, 8081),
        ("SCOREBOARD_GRPC", 29, 8081),
        ("SOUND_REQUEST_GRPC", 30, 8081),
        ("TTS_GRPC", 31, 8081),
        ("TELEGRAM_GRPC", 32, 8081),
    ];

    /// <summary>Шаблоны сред: в каждом набор портов обязан быть одним и тем же.</summary>
    private static readonly string[] Templates =
    [
        ".env.development.example",
        ".env.production.example",
    ];

    /// <summary>Compose-файлы: между ними и делится набор подставляемых переменных.</summary>
    private static readonly string[] ComposeFiles =
    [
        "docker-compose.yml",
        "docker-compose.dev.yml",
    ];

    /// <summary>
    /// Значение каждого порта — ровно <c>base + номер</c>.
    /// </summary>
    /// <remarks>
    /// Это ядро схемы: номер в <see cref="Scheme"/> — единственное место, где
    /// он задан, а шаблон обязан ему соответствовать. Проверка ловит и
    /// забытую переменную, и порт, оставшийся от прежней базы.
    /// </remarks>
    [Fact]
    public void КаждыйПортРавенБазовомуПлюсНомер()
    {
        foreach (var template in Templates)
        {
            var values = Read(template);
            var basePort = BasePort(values, template);

            foreach (var (key, index, _) in Scheme)
            {
                var name = "MARS_PORT_" + key;
                var port = Port(values, name, template);

                Assert.Equal(basePort + index, port);
            }
        }
    }

    /// <summary>Номера идут от нуля без пропусков, имена и порты не повторяются.</summary>
    /// <remarks>
    /// Пропуск в нумерации безобиден ровно до первого нового сервиса: он встанет
    /// на «свободный» номер, которого на деле нет, и молча получит чужой порт.
    /// </remarks>
    [Fact]
    public void НомераИдутОтНуляБезПропусков()
    {
        Assert.Equal(Enumerable.Range(0, Scheme.Length), Scheme.Select(entry => entry.Index));

        Assert.Equal(
            Scheme.Length,
            Scheme.Select(entry => entry.Key).Distinct(StringComparer.Ordinal).Count()
        );

        Assert.Equal(Scheme.Length, Scheme.Select(entry => entry.Index).Distinct().Count());
    }

    /// <summary>
    /// Базовые порты сред различаются: иначе два стенда не поднять одновременно.
    /// </summary>
    [Fact]
    public void БазыСредРазличаются()
    {
        var bases = Templates.Select(template => BasePort(Read(template), template)).ToList();

        Assert.NotEqual(bases[0], bases[1]);
    }

    /// <summary>Хостовые порты одной среды не повторяются и лежат вне системных.</summary>
    /// <remarks>
    /// Два сервиса на одном порту — это не «повезёт»: второй контейнер не
    /// поднимается вовсе, с «Bind for 0.0.0.0:&lt;порт&gt; failed». Проверка
    /// ловит это там, где порты нарезаны, а не на живом запуске.
    /// </remarks>
    [Fact]
    public void ПортыНеПовторяются()
    {
        foreach (var template in Templates)
        {
            var values = Read(template);
            var ports = Scheme
                .Select(entry => Port(values, "MARS_PORT_" + entry.Key, template))
                .ToList();

            Assert.Equal(ports.Count, ports.Distinct().Count());

            Assert.All(ports, port => Assert.InRange(port, 1024, 65535));
        }
    }

    /// <summary>
    /// Между двумя compose-файлами подставляется ровно объявленный набор портов.
    /// </summary>
    /// <remarks>
    /// Схема — это договорённость целиком: переменная без подстановки в compose
    /// не занимает порт (и легко забывается), а подстановка переменной вне
    /// схемы не проверяется ничем. Проверяются оба направления.
    /// </remarks>
    [Fact]
    public void ComposeПодставляетКаждыйПортСхемы()
    {
        var referenced = ComposeFiles
            .Select(file =>
                ComposeVariable()
                    .Matches(File.ReadAllText(ClientUiImageWorkflowTests.FindRepositoryFile(file)))
                    .Select(match => match.Groups["name"].Value)
                    .Where(name => name.StartsWith("MARS_PORT_", StringComparison.Ordinal))
            )
            .SelectMany(names => names)
            .ToHashSet(StringComparer.Ordinal);

        var expected = Scheme
            .Select(entry => "MARS_PORT_" + entry.Key)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, referenced.OrderBy(name => name, StringComparer.Ordinal));
    }

    /// <summary>
    /// Боевой стенд публикует наружу только Gateway.
    /// </summary>
    /// <remarks>
    /// Инфраструктура и сервисы наружу не выходят: к ним нечего подключаться
    /// снаружи, а открытый postgres на боевом хосте — это дыра, а не удобство.
    /// Единственная публикация остаётся на Gateway, и её номер берётся из
    /// переменной, а не зашит числом.
    /// </remarks>
    [Fact]
    public void БоевойСтендПубликуетТолькоГейтвей()
    {
        var published = PublishedPorts("docker-compose.yml");

        Assert.Equal(new[] { "${MARS_PORT_GATEWAY}:8080" }, published);
    }

    /// <summary>
    /// Стенд разработки публикует приложение и инфраструктуру по схеме.
    /// </summary>
    /// <remarks>
    /// Проверяется не «есть хоть одна публикация», а весь набор: номер и
    /// контейнерный порт каждой публикации обязаны совпасть со схемой — иначе
    /// <c>6024:3000</c>, набранное как <c>6024:3001</c>, дошло бы до стенда.
    /// </remarks>
    [Fact]
    public void СтендРазработкиПубликуетВсёПоСхеме()
    {
        var published = PublishedPorts("docker-compose.dev.yml");

        var expected = Scheme
            .Where(entry => entry.Key != "GATEWAY")
            .Select(entry => $"${{MARS_PORT_{entry.Key}}}:{entry.ContainerPort}")
            .OrderBy(spec => spec, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, published);
    }

    /// <summary>
    /// Перечень ключей содержит и базу, и все порты — иначе скрипт создаст файл
    /// окружения без них.
    /// </summary>
    [Fact]
    public void ПереченьКлючейСодержитПорты()
    {
        var values = Read(".env.example");

        Assert.True(values.ContainsKey("MARS_PORT_BASE"), "В .env.example нет MARS_PORT_BASE.");

        foreach (var (key, _, _) in Scheme)
        {
            Assert.True(
                values.ContainsKey("MARS_PORT_" + key),
                $"В .env.example нет MARS_PORT_{key}."
            );
        }
    }

    /// <summary>Базовый порт среды из файла окружения.</summary>
    private static int BasePort(Dictionary<string, string> values, string template)
    {
        Assert.True(values.ContainsKey("MARS_PORT_BASE"), $"{template}: нет MARS_PORT_BASE.");

        return Port(values, "MARS_PORT_BASE", template);
    }

    /// <summary>Обязательная целочисленная переменная из файла окружения.</summary>
    private static int Port(Dictionary<string, string> values, string name, string template)
    {
        Assert.True(values.TryGetValue(name, out var raw), $"{template}: нет переменной {name}.");

        Assert.True(
            int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var port),
            $"{template}: {name}={raw} — не целое число."
        );

        return port;
    }

    /// <summary>
    /// Публикуемые на хост порты из compose-файла, в порядке появления.
    /// </summary>
    /// <remarks>
    /// Разбор по отступу, а не регуляркой по всему тексту: <c>- "…"</c> есть и у
    /// <c>volumes</c>, и общий поиск вернул бы тома вперемешку с портами. Уровень
    /// сервиса — два пробела, ключ — четыре, элемент списка — шесть.
    /// </remarks>
    private static List<string> PublishedPorts(string file)
    {
        var result = new List<string>();
        var collecting = false;

        foreach (var raw in File.ReadAllLines(ClientUiImageWorkflowTests.FindRepositoryFile(file)))
        {
            var line = raw.TrimEnd('\r');

            if (line.Trim().Length == 0 || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            var indent = line.Length - line.TrimStart(' ').Length;
            var content = line.Trim();

            if (indent >= 6)
            {
                if (collecting && content.StartsWith("- ", StringComparison.Ordinal))
                {
                    result.Add(content[2..].Trim().Trim('"'));
                }

                continue;
            }

            // Секция верхнего уровня или заголовок сервиса закрывают список.
            collecting = indent == 4 && content == "ports:";
        }

        return result.OrderBy(spec => spec, StringComparer.Ordinal).ToList();
    }

    /// <summary>Читает файл окружения как словарь <c>КЛЮЧ=значение</c>.</summary>
    private static Dictionary<string, string> Read(string file)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var line in File.ReadAllLines(ClientUiImageWorkflowTests.FindRepositoryFile(file)))
        {
            var trimmed = line.Trim();

            if (trimmed.Length > 0 && trimmed[0] != '#')
            {
                var separator = trimmed.IndexOf('=');

                if (separator > 0)
                {
                    result[trimmed[..separator].Trim()] = trimmed[(separator + 1)..].Trim();
                }
            }
        }

        return result;
    }

    /// <summary>Имя переменной внутри <c>${…}</c>.</summary>
    [GeneratedRegex(@"\$\{(?<name>[A-Za-z0-9_]+)")]
    private static partial Regex ComposeVariable();
}
