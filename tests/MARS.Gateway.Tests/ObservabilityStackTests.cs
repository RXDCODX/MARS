using System.Text.RegularExpressions;

namespace MARS.Gateway.Tests;

/// <summary>
/// Наблюдаемость сервиса, запущенного на хосте: метрики в Prometheus и логи в Loki.
/// </summary>
/// <remarks>
/// <para>
/// Все три канала устроены по-разному, и каждый закрывается своим файлом:
/// метрики Prometheus опрашивает сам, трейсы приложение шлёт само в Tempo, а логи
/// доставляет Alloy, читающий stdout контейнеров. Контейнерные пути работают, но
/// процесс на хосте в них не виден: его <c>/metrics</c> не опрашивается, а вывод
/// консоли не является stdout контейнера.
/// </para>
/// <para>
/// Проверки идут по файлам конфигурации, а не по работающему стенду: стенд может
/// быть не поднят, а разъехавшаяся правка (нет монтирования, нет цели в задаче,
/// переменная дашборда без второго job) выглядит как «Grafana пустая», и искать
/// пришлось бы в самой Grafana.
/// </para>
/// </remarks>
public sealed partial class ObservabilityStackTests
{
    /// <summary>Каталог целей, который пишет сервис на хосте.</summary>
    private const string TargetsMount = "data/prometheus-local:/etc/prometheus/local";

    /// <summary>Каталог файлов логов, которые пишет сервис на хосте.</summary>
    private const string LogsMount = "data/local-logs:/var/log/mars";

    /// <summary>
    /// Задача <c>mars-local</c> читает цели из файла, а не из списка в конфиге:
    /// файл пишет сам запущенный сервис, поэтому в задаче ровно те процессы, что
    /// работают на хосте. Статичный список хостовых портов дал бы четырнадцать
    /// вечно недоступных целей, а при полностью docker-стенде — ещё и двойной
    /// сбор метрик по опубликованным портам.
    /// </summary>
    [Fact]
    public void Prometheus_Собирает_Хостовые_Процессы_По_Файлу()
    {
        var job = Job("mars-local");

        Assert.Contains("file_sd_configs", job, StringComparison.Ordinal);
        Assert.Contains("/etc/prometheus/local/*.json", job, StringComparison.Ordinal);
    }

    /// <summary>
    /// У задачи нет <c>relabel_configs</c>: лейбл <c>instance</c> приходит из
    /// файла вместе с именем службы. Перезапись по адресу подписала бы цель
    /// именем <c>host.docker.internal</c>, и переменная <c>$instance</c> дашборда
    /// показала бы адрес вместо сервиса.
    /// </summary>
    [Fact]
    public void У_Хостовой_Задачи_Нет_Перезаписи_Инстанса()
    {
        Assert.DoesNotContain("relabel_configs", Job("mars-local"), StringComparison.Ordinal);
    }

    /// <summary>
    /// Контейнерные цели остаются на месте: правка добавляет второй путь сбора, а
    /// не заменяет первый — иначе стенд целиком в Docker потерял бы метрики.
    /// </summary>
    [Fact]
    public void Контейнерные_Цели_На_Месте()
    {
        var job = Job("mars-services");

        Assert.Contains("gateway:8080", job, StringComparison.Ordinal);
        Assert.Contains("twitch-core:8080", job, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prometheus в контейнере достучивается до процесса на хосте по
    /// <c>host.docker.internal</c>: имя это знает Docker Desktop, а на Linux его
    /// даёт <c>host-gateway</c> из <c>extra_hosts</c> — тем же приёмом, что
    /// AudioController ходит к микшеру Windows.
    /// </summary>
    [Fact]
    public void Prometheus_Видит_Хост()
    {
        var block = ComposeFile.Block("prometheus", "docker-compose.dev.yml");

        Assert.Contains("host.docker.internal:host-gateway", block, StringComparison.Ordinal);
        Assert.Contains(TargetsMount, block, StringComparison.Ordinal);
    }

    /// <summary>
    /// Каталоги переживают перезапуск, но не попадают в git: иначе каждая отладка
    /// оставляла бы в репозитории файл целей и лог отлаживаемого сервиса.
    /// </summary>
    [Fact]
    public void Каталоги_Наблюдаемости_Под_Gitignore()
    {
        var ignored = File.ReadAllLines(
                ClientUiImageWorkflowTests.FindRepositoryFile(".gitignore")
            )
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && line[0] != '#' && line[0] != '!')
            .ToList();

        Assert.Contains("/data/prometheus-local/", ignored);
        Assert.Contains("/data/local-logs/", ignored);
    }

    /// <summary>
    /// Alloy получает логи процесса на хосте вторым источником, в тот же
    /// <c>loki.write</c>: отдельная запись в Loki была бы вторым путём доставки
    /// с собственными настройками, которые разъедутся с первым.
    /// </summary>
    [Fact]
    public void Alloy_Читает_Логи_Процессов_На_Хосте()
    {
        var config = ClientUiImageWorkflowTests.FindRepositoryFile(
            "infrastructure",
            "alloy",
            "config.alloy"
        );

        var text = File.ReadAllText(config);

        Assert.Contains("local.file_match", text, StringComparison.Ordinal);
        Assert.Contains("loki.source.file", text, StringComparison.Ordinal);
        Assert.Contains("loki.write.mars.receiver", text, StringComparison.Ordinal);

        // Лейбл container выводится из имени файла: сервис пишет лог под именем
        // службы compose, и запрос {container="telegram"} должен находить строки
        // и из контейнера, и с хоста.
        Assert.Contains(@"__path__", text, StringComparison.Ordinal);
        Assert.Contains(@"\.log", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Локальный стек, а не боевой: файловый сток логов и каталог целей нужны
    /// только отладке на хосте.
    /// </summary>
    [Fact]
    public void Dev_Оверрайд_Отдаёт_Все_Каталоги()
    {
        Assert.Contains(
            LogsMount,
            ComposeFile.Block("alloy", "docker-compose.dev.yml"),
            StringComparison.Ordinal
        );

        Assert.DoesNotContain(
            "prometheus-local",
            ComposeFile.Block("prometheus"),
            StringComparison.Ordinal
        );
    }

    /// <summary>
    /// Переменная <c>$instance</c> дашборда обязана видеть обе задачи: иначе
    /// отлаживаемый процесс не появился бы в списке, и панели остались бы пустыми
    /// при полностью рабочем Prometheus.
    /// </summary>
    [Fact]
    public void Переменная_Дашборда_Видит_И_Хостовые_Инстансы()
    {
        var dashboard = File.ReadAllText(
            ClientUiImageWorkflowTests.FindRepositoryFile(
                "infrastructure",
                "grafana",
                "dashboards",
                "mars-overview.json"
            )
        );

        Assert.Contains(
            "label_values(process_start_time_seconds{job=~\\\"mars-services|mars-local\\\"}, instance)",
            dashboard,
            StringComparison.Ordinal
        );
    }

    /// <summary>Блок задачи Prometheus по имени <c>job_name</c>.</summary>
    private static string Job(string name)
    {
        var text = File.ReadAllText(
            ClientUiImageWorkflowTests.FindRepositoryFile(
                "infrastructure",
                "prometheus",
                "prometheus.yml"
            )
        );

        var starts = JobStart().Matches(text).Cast<Match>().ToList();

        var current = starts.FindIndex(match => match.Groups["name"].Value == name);

        Assert.True(current >= 0, $"В prometheus.yml нет задачи {name}.");

        var start = starts[current].Index;
        var end = current + 1 < starts.Count ? starts[current + 1].Index : text.Length;

        return text[start..end];
    }

    [GeneratedRegex(@"^\s*-?\s*job_name:\s*'(?<name>[^']+)'", RegexOptions.Multiline)]
    private static partial Regex JobStart();
}
