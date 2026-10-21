using System.Text.RegularExpressions;

namespace MARS.Gateway.Tests;

/// <summary>
/// Наблюдаемость сервиса, запущенного на хосте: метрики в Prometheus и логи в Loki.
/// </summary>
/// <remarks>
/// <para>
/// Контейнерный стенд работает сам: Prometheus опрашивает имена compose-служб,
/// а Alloy читает их stdout через Docker-демон. Процесс на хосте в обе схемы не
/// попадает ни в первую, ни во вторую — и его метрики, и его логи приходится
/// доставлять отдельным путём.
/// </para>
/// <para>
/// Метрики и логи решаются по-разному и намеренно так: метрики опрашивает
/// внешний Prometheus, поэтому сервису достаточно записать свой порт в файл, а
/// логи приложение шлёт само по OTLP в Alloy, потому что Tempo логи не хранит и
/// прежняя отправка записей туда означала бы их потерю.
/// </para>
/// <para>
/// Проверки идут по файлам конфигурации, а не по работающему стенду: стенд
/// может быть не поднят, а разъехавшаяся правка (нет монтирования, нет цели в
/// задаче, переменная дашборда без второго job) выглядит как «Grafana пустая»,
/// и искать пришлось бы в самой Grafana.
/// </para>
/// </remarks>
public sealed partial class ObservabilityStackTests
{
    /// <summary>Каталог целей, который пишет сервис на хосте.</summary>
    private const string TargetsMount = "data/prometheus-local:/etc/prometheus/local";

    /// <summary>
    /// Метка, которой compose помечает сервисы, доставляющие логи по OTLP.
    /// </summary>
    private const string OtlpLogsLabel = "mars.logs";

    /// <summary>Значение метки <see cref="OtlpLogsLabel"/> у приложений.</summary>
    private const string OtlpLogsValue = "otlp";

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
    /// Правка добавляет вторую задачу, а не переписывает первую: иначе стенд
    /// целиком в Docker потерял бы собранные им метрики. Проверяется не только
    /// наличие целей, но и то, что у них остался <c>relabel_configs</c> —
    /// без него <c>instance</c> стал бы адресом <c>gateway:8080</c> и панели
    /// дашборда перестали бы совпадать с прежними.
    /// </summary>
    [Fact]
    public void Контейнерной_Задаче_Оставили_Перезапись_Инстанса()
    {
        Assert.Contains("relabel_configs", Job("mars-services"), StringComparison.Ordinal);
    }

    /// <summary>
    /// Задачи лежат в том порядке, в котором их читает Prometheus: <c>mars-local</c>
    /// идёт последней. Это не украшение: файл целей сервис дописывает на ходу, и
    /// вставка задачи в середину скрыла бы меняющийся каталог под чужим блоком.
    /// </summary>
    [Fact]
    public void Хостовая_Задача_Идёт_Последней()
    {
        var text = File.ReadAllText(
            ClientUiImageWorkflowTests.FindRepositoryFile(
                "infrastructure",
                "prometheus",
                "prometheus.yml"
            )
        );

        var starts = JobStart().Matches(text).Cast<Match>().ToList();

        Assert.Equal("mars-local", starts[^1].Groups["name"].Value);
    }

    /// <summary>
    /// Prometheus в контейнере достучивается до процесса на хосте по
    /// <c>host.docker.internal</c>: имя это знает Docker Desktop, а на Linux его
    /// даёт <c>host-gateway</c> из <c>extra_hosts</c> — тем же приёмом, что
    /// AudioController ходит к микшеру Windows. Каталог целей монтируется только
    /// в dev: на боевом стенде такого процесса нет, и пустой каталог сводил бы
    /// задачу к нулю целей при каждом деплое.
    /// </summary>
    [Fact]
    public void Prometheus_Видит_Хост()
    {
        var block = ComposeFile.Block("prometheus", "docker-compose.dev.yml");

        Assert.Contains("host.docker.internal:host-gateway", block, StringComparison.Ordinal);
        Assert.Contains(TargetsMount, block, StringComparison.Ordinal);
    }

    /// <summary>
    /// Боевой стенд хостового процесса не знает: там нет ни каталога целей, ни
    /// адреса хоста. Без этой проверки монтирование могло бы уехать в базовый
    /// файл, и боевой Prometheus начинал бы опрашивать машину, на которой
    /// запущен, — пустые цели при каждом деплое.
    /// </summary>
    [Fact]
    public void Боевой_Стенд_Хостового_Процесса_Не_Знает()
    {
        var block = ComposeFile.Block("prometheus");

        Assert.DoesNotContain("prometheus-local", block, StringComparison.Ordinal);
        Assert.DoesNotContain("host.docker.internal", block, StringComparison.Ordinal);
    }

    /// <summary>
    /// Каталог целей переживает перезапуск, но не попадает в git: иначе каждая
    /// отладка оставляла бы в репозитории файл с портом процесса автора.
    /// </summary>
    [Fact]
    public void Каталог_Целей_Под_Gitignore()
    {
        Assert.Contains("/data/prometheus-local/", IgnoredPaths());
    }

    /// <summary>
    /// OTLP-ветка Alloy обязана заканчиваться тем же <c>loki.write</c>, что и
    /// Docker-ветка: отдельная запись в Loki была бы вторым путём доставки с
    /// собственными настройками, которые разъедутся с первым.
    /// </summary>
    [Fact]
    public void Alloy_Принимает_OTLP_В_Тот_Же_Loki_Write()
    {
        var text = AlloyConfig();

        Assert.Contains("otelcol.receiver.otlp", text, StringComparison.Ordinal);
        Assert.Contains("otelcol.exporter.loki", text, StringComparison.Ordinal);
        Assert.Contains("loki.write.mars.receiver", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Вложенные ссылки на <c>loki.write.mars.receiver</c> обязаны совпасть:
    /// иначе OTLP-ветка пишет в Loki по-своему, а Docker-ветка — по-своему, и
    /// подсказка <c>loki.resource.labels</c> действует только на одну из них.
    /// </summary>
    [Fact]
    public void Alloy_Всё_Пишет_Через_Один_Приёмник_Loki()
    {
        var text = AlloyConfig();

        Assert.Equal(2, text.Split("loki.write.mars.receiver").Length - 1);
    }

    /// <summary>
    /// Подсказка <c>loki.resource.labels</c> обязана присутствовать: без неё
    /// <c>otelcol.exporter.loki</c> не превращает атрибуты OTLP-записи в метки
    /// Loki, и все записи одного сервиса сливаются в один поток — запрос
    /// <c>{container="telegram"}</c> не находит ничего.
    /// </summary>
    [Fact]
    public void Alloy_Разворачивает_Атрибуты_OTLP_В_Метки_Loki()
    {
        Assert.Contains("loki.resource.labels", AlloyConfig(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Логи приложения не читаются из stdout, иначе каждая строка попала бы в
    /// Loki дважды: один раз как OTLP-запись, второй — как строка контейнера.
    /// Роль выполняет метка <c>mars.logs</c>, которую ставит якорь сервисов, а
    /// отбрасывает такие контейнеры правило <c>drop</c> в
    /// <c>discovery.relabel</c>.
    ///
    /// Проверяется именно блок <c>discovery.relabel "mars"</c>, а не файл
    /// целиком: под строку <c>action</c> в файле подходит и
    /// <c>action { key = "mars.logs" action = "delete" }</c> из
    /// <c>otelcol.processor.attributes</c>, из-за чего прежняя проверка всего
    /// файла проходила зелёной при полностью отсутствующем <c>drop</c>.
    /// </summary>
    [Fact]
    public void Alloy_Не_Дублирует_OTLP_Логи_Из_Stdout()
    {
        var block = AlloyBlock("discovery.relabel", "mars");

        Assert.Contains("__meta_docker_container_label_mars_logs", block, StringComparison.Ordinal);
        Assert.Contains(OtlpLogsValue, block, StringComparison.Ordinal);
        Assert.Contains("action", block, StringComparison.Ordinal);
        Assert.Contains("drop", block, StringComparison.Ordinal);
    }

    /// <summary>
    /// Отбрасывание stdout работает только у контейнеров, которые шлют логи
    /// сами: метка <c>mars.logs=otlp</c> есть ровно у приложений, а у
    /// инфраструктуры (postgres, rabbitmq, loki, grafana) её нет, и её логи
    /// остаются единственным источником в Loki. Поэтому источник stdout обязан
    /// остаться в конфиге, а <c>drop</c> — опираться на метку контейнера.
    /// </summary>
    [Fact]
    public void Alloy_Читает_Stdout_У_Контейнеров_Без_Метки_OTLP()
    {
        var text = AlloyConfig();

        Assert.Contains("loki.source.docker", text, StringComparison.Ordinal);

        // Источник stdout подписан на результат relabel, а не на пустой список:
        // иначе логи инфраструктуры (у неё метки mars.logs нет) не доходили бы
        // никуда — их не отправить по OTLP, они читаются только из stdout.
        var source = AlloyBlock("loki.source.docker", "mars");
        Assert.Contains("discovery.relabel.mars.output", source, StringComparison.Ordinal);
        Assert.Contains("loki.write.mars.receiver", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Тот же якорь окружения выставляет сервисам адрес OTLP-логов. Следствие:
    /// приложение, запущенное в контейнере, шлёт логи в Alloy, а не в Tempo —
    /// иначе записи терялись бы, потому что Tempo логи не хранит.
    /// </summary>
    [Fact]
    public void Сервисы_В_Контейнере_Шлют_Логи_В_Alloy()
    {
        // Читается файл целиком, а не блоком сервиса: якорь лежит на строке
        // `x-service-defaults: &service-defaults`, а блок определяется по строке
        // вида `  имя:` — якорь им не является.
        var compose = File.ReadAllText(
            ClientUiImageWorkflowTests.FindRepositoryFile("docker-compose.yml")
        );

        Assert.Contains("Serilog__OtlpEndpoint", compose, StringComparison.Ordinal);
        Assert.Contains(OtlpLogsLabel + ": " + OtlpLogsValue, compose, StringComparison.Ordinal);
    }

    /// <summary>
    /// Порт OTLP-логов Alloy публикуется отдельно от его UI: на хосте приложение
    /// ходит по <c>localhost</c>, а внутри сети — по имени службы на том же
    /// номере контейнера. Один порт на обе цели не работает: UI Alloy слушает
    /// 12345, а приёмник OTLP — 4317.
    /// </summary>
    [Fact]
    public void Порт_OTLP_Alloy_Публикуется_В_Dev()
    {
        var block = ComposeFile.Block("alloy", "docker-compose.dev.yml");

        Assert.Contains("4317", block, StringComparison.Ordinal);
    }

    /// <summary>
    /// Локальный стенд, а не боевой: каталог целей и хостовый адрес нужны только
    /// отладке на хосте.
    /// </summary>
    [Fact]
    public void Dev_Оверрайд_Отдаёт_Все_Каталоги()
    {
        Assert.Contains(
            TargetsMount,
            ComposeFile.Block("prometheus", "docker-compose.dev.yml"),
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

    /// <summary>
    /// Панель с метриками процесса обязана спрашивать у Prometheus задачи
    /// <c>mars-local</c>: иначе CPU и память отлаживаемого процесса в дашборде
    /// не появятся, хотя Prometheus его уже опрашивает.
    /// </summary>
    [Fact]
    public void Панель_Процесса_Спрашивает_И_Хостовую_Задачу()
    {
        var dashboard = File.ReadAllText(
            ClientUiImageWorkflowTests.FindRepositoryFile(
                "infrastructure",
                "grafana",
                "dashboards",
                "mars-overview.json"
            )
        );

        Assert.Contains("process_cpu_seconds_total", dashboard, StringComparison.Ordinal);
        Assert.Contains("process_start_time_seconds", dashboard, StringComparison.Ordinal);
    }

    /// <summary>Непустые незакомментированные пути из <c>.gitignore</c>.</summary>
    private static List<string> IgnoredPaths()
    {
        return File.ReadAllLines(ClientUiImageWorkflowTests.FindRepositoryFile(".gitignore"))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && line[0] != '#' && line[0] != '!')
            .ToList();
    }

    /// <summary>Текст конфигурации Alloy.</summary>
    private static string AlloyConfig()
    {
        return File.ReadAllText(
            ClientUiImageWorkflowTests.FindRepositoryFile("infrastructure", "alloy", "config.alloy")
        );
    }

    /// <summary>
    /// Блок компонента Alloy по имени и метке: <c>discovery.relabel "mars" { … }</c>.
    /// Границы — от строки объявления до закрывающей скобки в первой колонке
    /// (вложенные <c>rule { … }</c> закрываются с отступом). Окончания строк
    /// могут быть CRLF, поэтому сравнение идёт без привязки к ним.
    /// </summary>
    private static string AlloyBlock(string component, string label)
    {
        var text = AlloyConfig();
        var header = component + " \"" + label + "\"";

        var start = text.IndexOf(header, StringComparison.Ordinal);
        Assert.True(start >= 0, $"В config.alloy нет компонента {header}.");

        // Граница ищется по самому тексту, а не накоплением смещения строк:
        // накопление считало бы длину строки без «\r» и на CRLF-файле (checkout
        // на Windows) уходило бы на байт на строку — блок обрезался бы на
        // последних строках, и «drop» в нём не находился бы. Закрывающая скобка
        // компонента стоит в первой колонке: вложенные блоки смещены, поэтому
        // первая же «\n}» — это конец компонента.
        var closing = text.IndexOf("\n}", start, StringComparison.Ordinal);

        Assert.True(closing > start, $"У компонента {header} не найдена закрывающая скобка.");

        var end = Math.Min(closing + 2, text.Length);

        return text[start..end];
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
