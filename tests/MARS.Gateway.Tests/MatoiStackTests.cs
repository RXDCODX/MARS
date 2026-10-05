namespace MARS.Gateway.Tests;

/// <summary>
/// Договорённости стека matoi в <c>docker-compose.yml</c>, prometheus и Grafana.
/// </summary>
/// <remarks>
/// Проверки выведены из живых прогонов контейнеров
/// <c>ghcr.io/sinkaroid/matoi:15.7.0-alpha</c> и
/// <c>ghcr.io/sinkaroid/matoi-redis:latest</c>, а не из документации: почти
/// каждое утверждение ниже противоречит тому, что ожидаешь по аналогии с
/// остальным стеком.
/// </remarks>
public class MatoiStackTests
{
    /// <summary>
    /// Оба контейнера обязаны быть в <c>mars-network</c> и перезапускаться.
    /// </summary>
    /// <remarks>
    /// Блоки новых сервисов намеренно не наследуют <c>x-service-defaults</c>:
    /// якорь тянет за собой .NET-окружение, healthcheck на <c>:8080</c> и
    /// <c>depends_on</c> на postgres с rabbitmq, которых у matoi нет. Вместе с
    /// якорем уходит и <c>restart</c>, а без него контейнер, упавший на
    /// неготовом Redis, остаётся мёртвым — и compose <c>--wait</c> отдаётся
    /// «dependency failed to start». Сеть то же самое: вне <c>mars-network</c>
    /// <c>alerts</c> и <c>telegram</c> не видят matoi по имени и получают
    /// <c>Connection refused</c> при каждой награде.
    /// </remarks>
    [Fact]
    public void MatoiОбслуживаетсяКакЧастьСтека()
    {
        foreach (var service in new[] { "matoi", "matoi-redis" })
        {
            var block = ComposeFile.Block(service);

            Assert.True(
                block.Contains("restart: unless-stopped"),
                $"Сервис {service} не перезапускается после сбоя:" + Environment.NewLine + block
            );

            Assert.True(
                block.Contains("mars-network"),
                $"Сервис {service} не включён в mars-network, поэтому его не видят"
                    + " по имени ни alerts, ни telegram:"
                    + Environment.NewLine
                    + block
            );
        }
    }

    /// <summary>
    /// Образы matoi закреплены: у redis только digest, у matoi — версионный тег.
    /// </summary>
    /// <remarks>
    /// Проверено на живом стенде, где matoi-redis тянул ровно этот digest, и на
    /// самом реестре: <c>/v2/sinkaroid/matoi-redis/tags/list</c> отдаёт единственный
    /// тег <c>latest</c>, версионного нет. Остальные образы стенда держат
    /// версионный тег, поэтому <c>latest</c> здесь был бы единственным местом,
    /// где поведение меняется вместе с <c>docker compose pull</c> — и меняется без
    /// единой записи в репозитории.
    /// </remarks>
    [Fact]
    public void ОбразыMatoiЗакреплены()
    {
        var redis = Image("matoi-redis");
        var matoi = Image("matoi");

        // Digest здесь не блажь, а единственный вариант: тега с версией не
        // существует, и «закрепить» его нечем.
        Assert.Equal("ghcr.io/sinkaroid/matoi-redis", redis.Split('@')[0]);
        Assert.Matches("^sha256:[a-f0-9]{64}$", redis.Split('@')[1]);

        Assert.Equal("ghcr.io/sinkaroid/matoi:15.7.0-alpha", matoi);
        Assert.DoesNotContain("latest", matoi, StringComparison.Ordinal);
    }

    /// <summary>
    /// Достаёт значение <c>image</c> из блока сервиса.
    /// </summary>
    private static string Image(string service)
    {
        var block = ComposeFile.Block(service);
        var line = block
            .Split('\n')
            .Select(entry => entry.Trim())
            .FirstOrDefault(entry => entry.StartsWith("image:", StringComparison.Ordinal));

        Assert.NotNull(line);

        return line!["image:".Length..].Trim();
    }

    /// <summary>
    /// matoi обязан дождаться готового redis.
    /// </summary>
    /// <remarks>
    /// Redis у matoi не опционален: <c>main.go</c> печатает
    /// <c>Failed to initialize Redis cache</c> и уходит с кодом 1, если
    /// <c>cache.InitRedis</c> не прошёл <c>Ping</c>. Проверено на живом
    /// контейнере: без redis процесс завершается за секунду с
    /// <c>dial tcp 127.0.0.1:6379: connect: connection refused</c>.
    /// <c>restart: unless-stopped</c> тут не помощник — compose запускает
    /// зависимости по <c>service_healthy</c>, а не по факту рестарта.
    /// </remarks>
    [Fact]
    public void MatoiЖдётГотовыйRedis()
    {
        var block = ComposeFile.Block("matoi");

        Assert.True(
            block.Contains("matoi-redis"),
            "Сервис matoi не объявляет зависимость от matoi-redis, а matoi"
                + " завершается с кодом 1, если Redis недоступен:"
                + Environment.NewLine
                + block
        );

        Assert.True(
            block.Contains("condition: service_healthy"),
            "Зависимость matoi от matoi-redis объявлена без проверки готовности,"
                + " поэтому matoi стартует раньше, чем redis научился отвечать:"
                + Environment.NewLine
                + block
        );
    }

    /// <summary>
    /// Healthcheck matoi обязан стучаться по IPv4-адресу и уметь, что есть в образе.
    /// </summary>
    /// <remarks>
    /// Две ловушки, обе проверены на живом контейнере. Первая: matoi слушает
    /// <c>0.0.0.0:3000</c>, то есть только IPv4, а <c>localhost</c> в контейнере
    /// резолвится в <c>::1</c> — <c>wget http://localhost:3000/ping</c> отвечает
    /// <c>Connection refused</c>, тогда как <c>127.0.0.1</c> отвечает
    /// <c>{"success":true,"status":"ok"}</c>. Healthcheck с <c>localhost</c> был бы
    /// вечно красным при полностью рабочем сервисе. Вторая: в образе есть
    /// busybox <c>wget</c> (<c>/usr/bin/wget</c>) и нет <c>curl</c>, поэтому
    /// привычный <c>curl -fsS</c> из <c>x-service-defaults</c> завершился бы
    /// кодом 127. Эндпоинта <c>/health</c> у matoi тоже нет — только <c>/ping</c>,
    /// и он публичен, в отличие от <c>/api/*</c>.
    /// </remarks>
    [Fact]
    public void MatoiПроверяетсяПоIpv4ЧерезWget()
    {
        var test = ComposeFile.HealthcheckTest(ComposeFile.Block("matoi"));

        Assert.True(
            test.Contains("127.0.0.1"),
            "Healthcheck matoi стучится не по 127.0.0.1. matoi слушает только IPv4,"
                + " а localhost в контейнере резолвится в ::1 и даёт Connection refused:"
                + Environment.NewLine
                + "  "
                + test
        );

        Assert.True(
            test.Contains("/ping"),
            "Healthcheck matoi не проверяет /ping — единственный публичный эндпоинт"
                + " живости у matoi, /health не существует:"
                + Environment.NewLine
                + "  "
                + test
        );

        Assert.True(
            test.Contains("wget"),
            "Healthcheck matoi не использует wget. В образе ghcr.io/sinkaroid/matoi"
                + " есть busybox wget и нет curl, поэтому curl завершился бы кодом 127:"
                + Environment.NewLine
                + "  "
                + test
        );

        Assert.False(
            test.Contains("curl"),
            "Healthcheck matoi вызывает curl, которого в образе нет:"
                + Environment.NewLine
                + "  "
                + test
        );
    }

    /// <summary>
    /// Healthcheck redis обязан проверять <c>PONG</c>, а не код возврата.
    /// </summary>
    /// <remarks>
    /// Ловушка, воспроизведённая на живом контейнере: <c>redis-cli ping</c> без
    /// пароля печатает <c>NOAUTH Authentication required.</c> и
    /// <strong>возвращает код 0</strong>. Healthcheck, написанный как
    /// <c>redis-cli ping</c>, объявит контейнер здоровым при redis, который
    /// отвергает вообще всё. Проверять надо <c>PONG</c>:
    /// <c>redis-cli -a ... --no-auth-warning ping | grep -q PONG</c> — он возвращает
    /// 1 и на <c>NOAUTH</c>, и на <c>WRONGPASS</c>, и 0 только на настоящем ответе.
    /// </remarks>
    [Fact]
    public void RedisПроверяетPongАНеКодВозврата()
    {
        var test = ComposeFile.HealthcheckTest(ComposeFile.Block("matoi-redis"));

        Assert.True(
            test.Contains("grep -q PONG"),
            "Healthcheck matoi-redis не проверяет PONG. redis-cli ping без пароля"
                + " печатает NOAUTH и возвращает код 0, поэтому compose посчитал бы"
                + " контейнер здоровым при отвергающем всё redis:"
                + Environment.NewLine
                + "  "
                + test
        );
    }

    /// <summary>
    /// Пароль redis обязан приходить из окружения, а не оставаться зашитым в образ.
    /// </summary>
    /// <remarks>
    /// В образе <c>ghcr.io/sinkaroid/matoi-redis</c> стоит
    /// <c>CMD ["redis-server", "--requirepass", "matoi"]</c>, то есть пароль
    /// зашит в образ и не совпадает ни с чем в <c>.env</c>. Своего
    /// <c>command:</c> у compose нет — переопределять нечем, и стенд поднимется с
    /// паролем из образа, который никто не задавал. Пароль подставляется на этапе
    /// разбора compose, поэтому в healthcheck пишется то же выражение, а не
    /// <c>$$</c>: в exec-форме <c>command</c> префикс <c>$$</c> не раскрывается
    /// оболочкой, и redis получил бы literal-строку.
    /// </remarks>
    [Fact]
    public void RedisПарольПриходитИзОкружения()
    {
        var block = ComposeFile.Block("matoi-redis");

        Assert.True(
            block.Contains("MATOI_REDIS_PASSWORD"),
            "Пароль redis не задан через переменную окружения и остаётся зашитым"
                + " в образ (--requirepass matoi):"
                + Environment.NewLine
                + block
        );

        Assert.True(
            block.Contains("--requirepass ${MATOI_REDIS_PASSWORD")
                || block.Contains("--requirepass\", \"${MATOI_REDIS_PASSWORD")
                || block.Contains("--requirepass', '${MATOI_REDIS_PASSWORD"),
            "command matoi-redis не переопределяет requirepass значением из .env."
                + " Без этого стенд поднимается с паролем из образа:"
                + Environment.NewLine
                + block
        );
    }

    /// <summary>
    /// matoi не публикуется наружу.
    /// </summary>
    /// <remarks>
    /// Наружу выходит только Gateway (<c>10155:8080</c>), и matoi — внутренний
    /// шлюз к booru: его собственная авторизация статическим ключом не заменяет
    /// ни сетевую изоляцию, ни YARP-маршрут. Порт 3000 на хосте к тому же занят
    /// контейнером <c>cryptpad</c> из другого проекта, и публикация падала бы с
    /// «Bind for 0.0.0.0:3000 failed».
    /// </remarks>
    [Fact]
    public void MatoiНеПубликуетПортНаХост()
    {
        var block = ComposeFile.Block("matoi");

        Assert.False(
            block.Contains("ports:"),
            "У matoi появилась секция ports. matoi — внутренний сервис, наружу"
                + " выходит только Gateway, а порт 3000 на хосте занят cryptpad:"
                + Environment.NewLine
                + block
        );
    }

    /// <summary>
    /// Gateway публикуется на 10155 — это наружная точка входа для приложения.
    /// </summary>
    /// <remarks>
    /// Значение проверяется точным, а не «есть ports»: тесты клиента
    /// <c>MARS.ClientUi.Tests</c> ходят по своему <c>DefaultBaseUrl</c>, и при
    /// расхождении они бы не упали, а пошли бы проверять любой процесс,
    /// слушающий тот же порт.
    /// </remarks>
    [Fact]
    public void GatewayПубликуетсяНа10155()
    {
        var ports = PublishedPorts().Where(entry => entry.Name == "gateway").ToArray();

        Assert.Equal(["10155:8080"], ports.Select(entry => string.Join(", ", entry.Ports)));
    }

    /// <summary>
    /// Порт 9155 не публикуется ни одним сервисом.
    /// </summary>
    /// <remarks>
    /// Обход всего compose, а не блока Gateway. На машине разработчика 9155
    /// держит процесс старого монолита <c>MARS.Server</c> (репозиторий
    /// <c>MARS_old</c>, в этом репозитории его нет), и публикация на нём
    /// падала бы с «Bind for 0.0.0.0:9155 failed» — либо, что хуже, стенд
    /// молча отвечал бы из чужого процесса и тесты клиента прошли бы, ничего
    /// не проверяя.
    /// </remarks>
    [Fact]
    public void Порт9155НигдеНеПубликуется()
    {
        var busy = PublishedPorts()
            .SelectMany(entry =>
                entry
                    .Ports.Where(port => port.StartsWith("9155:", StringComparison.Ordinal))
                    .Select(port => $"{entry.Name}: {port}")
            )
            .ToArray();

        Assert.Equal([], busy);
    }

    /// <summary>
    /// Публикации портов по всему compose.
    /// </summary>
    /// <remarks>
    /// Сервис начинается строкой ровно с двумя пробелами и словом с двоеточием;
    /// вложенные ключи уходят глубже, а пояса верхнего уровня
    /// (<c>services</c>, <c>networks</c>, <c>x-service-defaults</c>) — в ноль.
    /// Пороги в списке публикаций встречаются и в кавычках (<c>"5432:5432"</c>),
    /// и без них, поэтому разбираются оба вида: иначе в списке молча терялась бы
    /// половина публикаций, и проверка «порт не публикуется» проходила бы в том
    /// числе для postgres.
    /// </remarks>
    private static List<(string Name, List<string> Ports)> PublishedPorts()
    {
        var published = new List<(string, List<string>)>();
        var name = string.Empty;
        List<string>? ports = null;

        foreach (
            var line in File.ReadAllLines(
                    ClientUiImageWorkflowTests.FindRepositoryFile("docker-compose.yml")
                )
                .Select(entry => entry.TrimEnd('\r'))
        )
        {
            var isServiceStart =
                line.Length > 2
                && line.StartsWith("  ", StringComparison.Ordinal)
                && !line.StartsWith("   ", StringComparison.Ordinal)
                && line.EndsWith(":", StringComparison.Ordinal);

            if (isServiceStart)
            {
                if (ports is { Count: > 0 })
                {
                    published.Add((name, ports));
                }

                name = line.Trim()[..^1];
                ports = null;
                continue;
            }

            if (line.Trim() == "ports:")
            {
                ports = [];
                continue;
            }

            if (ports is not null && line.TrimStart().StartsWith("- ", StringComparison.Ordinal))
            {
                ports.Add(line.Trim()[2..].Trim('"'));
            }
        }

        if (ports is { Count: > 0 })
        {
            published.Add((name, ports));
        }

        return published;
    }

    /// <summary>
    /// Сервисы-потребители обязаны получать адрес matoi и ключ авторизации.
    /// </summary>
    /// <remarks>
    /// <c>/api/*</c> у matoi закрыт middleware: без ключа и с неверным ключом
    /// ответ <c>401</c> (проверено на живом контейнере). Ключ приходит как
    /// <c>Authorization: Bearer</c>, поэтому обе переменные нужны каждому
    /// потребителю, а не только одному из них. Пока matoi не публикуется наружу,
    /// YARP-маршрут и свойство в <c>ServiceEndpoints</c> не нужны: на 3000 идёт
    /// только внутренняя сеть.
    /// </remarks>
    [Theory]
    [InlineData("alerts")]
    [InlineData("telegram")]
    public void ПотребительПолучаетАдресИКлючMatoi(string service)
    {
        var block = ComposeFile.Block(service);

        Assert.True(
            block.Contains("Matoi__BaseUrl"),
            $"Сервис {service} не получает Matoi__BaseUrl:" + Environment.NewLine + block
        );

        Assert.True(
            block.Contains("Matoi__ApiKey"),
            $"Сервис {service} не получает Matoi__ApiKey. Без ключа matoi отвечает 401"
                + " на каждый /api/* запрос:"
                + Environment.NewLine
                + block
        );
    }

    /// <summary>
    /// matoi обязан попадать в prometheus отдельной задачей.
    /// </summary>
    /// <remarks>
    /// Отдельной, потому что имя метрики у matoi совпадает с prometheus-net:
    /// оба зовут гистограмму <c>http_request_duration_seconds</c>. В одном job
    /// отличить их нечем, кроме <c>instance</c>, а переменная <c>$instance</c> в
    /// <c>mars-overview.json</c> собрана как
    /// <c>label_values(process_start_time_seconds, instance)</c> — а эту метрику
    /// matoi тоже отдаёт, через стандартные коллекторы Go-клиента. Без раздельных
    /// job matoi попадал бы в список .NET-сервисов дашборда, и панели p50/p95/p99
    /// для выбранного инстанса молча смешивали бы латентность Go-сервиса с .NET.
    /// </remarks>
    [Fact]
    public void MatoiСобираетсяОтдельнойЗадачейPrometheus()
    {
        var prometheus = File.ReadAllText(
            ClientUiImageWorkflowTests.FindRepositoryFile(
                "infrastructure",
                "prometheus",
                "prometheus.yml"
            )
        );

        Assert.Contains("matoi:3000", prometheus, StringComparison.Ordinal);
        Assert.Contains("matoi", prometheus, StringComparison.Ordinal);

        var marsServices = Between(prometheus, "mars-services", "job_name:");
        Assert.False(
            marsServices.Contains("matoi:3000"),
            "matoi попал в задачу mars-services. Имя его гистограммы"
                + " http_request_duration_seconds совпадает с prometheus-net, и"
                + " разделить их можно только по job:"
                + Environment.NewLine
                + marsServices
        );
    }

    /// <summary>
    /// Переменная <c>$instance</c> дашборда обязана исключать matoi.
    /// </summary>
    /// <remarks>
    /// Иначе Go-сервис появится в выпадающем списке рядом с .NET-сервисами, и
    /// панели латентности для него посчитают <c>sum by (le)</c> по бейкетам
    /// matoi, смешав их с бейкетами выбранного .NET-инстанса.
    /// </remarks>
    [Fact]
    public void ПеременнаяInstanceДашбордаИсключаетMatoi()
    {
        var dashboard = File.ReadAllText(
            ClientUiImageWorkflowTests.FindRepositoryFile(
                "infrastructure",
                "grafana",
                "dashboards",
                "mars-overview.json"
            )
        );

        Assert.True(
            dashboard.Contains(
                // Файл читается как текст, а не разбирается JSON, поэтому ищется
                // ровно то, что в нём написано: экранированная кавычка внутри
                // JSON-строки, а не голая. Разбор здесь ничего не проверял бы —
                // нужно убедиться именно в наличии фильтра по job в выражении.
                "label_values(process_start_time_seconds{job=\\\"mars-services\\\"}, instance)",
                StringComparison.Ordinal
            ),
            "Переменная $instance дашборда не отфильтрована по job. matoi отдаёт"
                + " process_start_time_seconds через стандартные Go-коллекторы и"
                + " попадёт в список .NET-сервисов."
        );
    }

    /// <summary>
    /// Новые переменные обязаны быть в обоих шаблонах окружений.
    /// </summary>
    /// <remarks>
    /// Окружения теперь два: <c>.env.development.example</c> и
    /// <c>.env.production.example</c>. Реальные файлы в git не попадают, шаблоны
    /// попадают, а compose получает нужный флагом <c>--env-file</c>. Без строки в
    /// обоих шаблонах стенд поднимется с <c>MATOI_API_KEY</c> пустым — matoi при
    /// пустом ключе вообще не спрашивает авторизацию и становится открытым внутри
    /// сети. Проверка одного шаблона была бы половиной договора: забытый
    /// <c>.env.production.example</c> обрушил бы боевой стенд на старте.
    /// </remarks>
    [Fact]
    public void ПеременныеMatoiЕстьВШаблонахОкружений()
    {
        foreach (var template in new[] { ".env.development.example", ".env.production.example" })
        {
            var env = File.ReadAllText(ClientUiImageWorkflowTests.FindRepositoryFile(template));

            Assert.True(
                env.Contains("MATOI_API_KEY"),
                $"В {template} нет MATOI_API_KEY. При пустом ключе matoi не включает"
                    + " авторизацию вовсе."
            );

            Assert.True(
                env.Contains("MATOI_REDIS_PASSWORD"),
                $"В {template} нет MATOI_REDIS_PASSWORD — redis поднимется с паролем"
                    + " из образа, а matoi сможет ходить в кэш."
            );
        }
    }

    /// <summary>Текст между началом совпадения и следующим вхождением маркера.</summary>
    /// <remarks>
    /// Разделитель выбран как начало следующей задачи: блок <c>mars-services</c>
    /// в файле заканчивается там, где начинается следующий <c>- job_name:</c>.
    /// </remarks>
    private static string Between(string text, string from, string to)
    {
        var start = text.IndexOf(from, StringComparison.Ordinal);
        var end = start < 0 ? -1 : text.IndexOf(to, start, StringComparison.Ordinal);

        return start < 0 || end < 0 ? string.Empty : text[start..end];
    }
}
