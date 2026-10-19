using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace MARS.Gateway.Tests;

/// <summary>
/// Профиль локального запуска <c>MARS.Local</c>: сервис поднимается на хосте, а
/// инфраструктура живёт в Docker.
/// </summary>
/// <remarks>
/// <para>
/// Профили — генерируемые файлы (<c>scripts/local_launch.py</c> из
/// <c>scripts/local-launch.conf</c>), то есть проверять их содержимое по одному
/// руками бессмысленно: источник правды — данные, а не текст. Но текст лежит в
/// репозитории и общий, поэтому тест ловит то, из-за чего сервис на хосте не
/// поднимется, а генератор этого не заметит.
/// </para>
/// <para>
/// Главная проверка — не «перегенерация совпадает с файлом» (её делает
/// <c>local --check</c>), а согласованность профилей между собой: адрес соседа в
/// одном профиле обязан совпасть с портом, на котором сосед слушает по своему
/// профилю. Это ловит и расхождение карты адресов с картой портов, и сервис,
/// забытый в общем блоке, — то есть ошибки данные-против-данных, а не
/// данные-против-копии.
/// </para>
/// <para>
/// Файлы читаются через <see cref="ClientUiImageWorkflowTests.FindRepositoryFile"/>
/// и разбираются <see cref="JsonDocument"/> — так проверка не зависит от того,
/// какой генератор (PowerShell, bash или Python) записал файл и как он его
/// отформатировал.
/// </para>
/// </remarks>
public sealed partial class LocalLaunchSettingsTests
{
    /// <summary>Префикс имени каталога сервиса; имя свойства в ServiceEndpoints — без него.</summary>
    private const string ServicePrefix = "MARS.";

    /// <summary>Свойства окружения, по которым сверяются профили между собой.</summary>
    private const string EndpointPrefix = "ServiceEndpoints__";

    private const string GrpcEndpointPrefix = "Grpc__Endpoints__";

    /// <summary>
    /// Хост, на котором сервис слушает в профиле запуска: <c>+</c> вместо
    /// localhost.
    /// </summary>
    /// <remarks>
    /// <c>localhost</c> — это 127.0.0.1, и до процесса, запущенного на хосте, из
    /// контейнера не достучаться: Prometheus, опрашивающий <c>/metrics</c>, идёт по
    /// <c>host.docker.internal</c> на адрес внешнего интерфейса. С 6-ю сервисами с
    /// gRPC проблемы нет — <c>AddMarsGrpcHosting</c> слушает <c>ListenAnyIP</c>.
    /// </remarks>
    private const string WildcardHost = "+";

    /// <summary>Данные профиля: общий блок плюс перекрытия сервисов и строки `#secrets`.</summary>
    private sealed record LaunchData(
        Dictionary<string, string> Common,
        Dictionary<string, Dictionary<string, string>> Services,
        Dictionary<string, string> Secrets
    );

    /// <summary>Профиль одного сервиса, как он записан в файле.</summary>
    private sealed record ProfileView(
        string Service,
        Dictionary<string, string> Variables,
        string ApplicationUrl
    );

    /// <summary>У каждого сервиса с <c>launchSettings.json</c> есть профиль MARS.Local.</summary>
    [Fact]
    public void ПрофильЕстьУКаждогоСервиса()
    {
        var profiles = ReadProfiles();

        Assert.Equal(ServiceDirectories().Count, profiles.Count);

        foreach (var profile in profiles)
        {
            var url = ApplicationUrl().Match(profile.ApplicationUrl);

            Assert.True(url.Success, $"{profile.Service}: applicationUrl разобран как не адрес.");
            Assert.Equal("http", url.Groups["scheme"].Value);
            Assert.Equal(WildcardHost, url.Groups["host"].Value);
            Assert.NotEmpty(profile.Variables);
        }
    }

    /// <summary>Адрес соседа в профиле совпадает с портом соседа в его собственном профиле.</summary>
    /// <remarks>
    /// «Сосед» берётся из <c>ServiceEndpoints__Имя</c>: сервис с профилем
    /// <c>MARS.Имя</c> обязан слушать тот порт, который назвали остальные. Если
    /// карта портов и карта адресов разошлись, локальный запуск двух сервисов
    /// молча не увидит друг друга — обе программы стартуют успешно.
    /// </remarks>
    [Fact]
    public void АдресСоседаСовпадаетСЕгоПортом()
    {
        var profiles = ReadProfiles();
        var byService = profiles.ToDictionary(profile => profile.Service, StringComparer.Ordinal);

        foreach (var (name, port) in EndpointPorts(profiles))
        {
            var service = ServicePrefix + name;

            Assert.True(
                byService.TryGetValue(service, out var own),
                $"Профили называют адрес {name}, а сервиса {service} в src/ нет."
            );

            Assert.Equal(port, ApplicationPort(own!.ApplicationUrl, own.Service));
        }
    }

    /// <summary>gRPC-порт профиля совпадает с gRPC-адресом, который называют соседи.</summary>
    /// <remarks>
    /// gRPC-адреса живут в секции <c>Grpc</c>, а не в <c>ServiceEndpoints</c>:
    /// Swagger-агрегатор строит карту рефлексией по строковым свойствам
    /// ServiceEndpoints и полез бы за спекой на порт, обслуживающий только HTTP/2.
    /// Проверяются обе стороны: названный соседями <c>Grpc__Endpoints__Имя</c> и
    /// собственные <c>Grpc__GrpcPort</c> (HTTP/2) и <c>Grpc__HttpPort</c> (HTTP/1.1).
    /// </remarks>
    [Fact]
    public void ПортGrpcСовпадаетСАдресомСоседа()
    {
        var profiles = ReadProfiles();
        var byService = profiles.ToDictionary(profile => profile.Service, StringComparer.Ordinal);

        foreach (var (name, port) in GrpcEndpointPorts(profiles))
        {
            var service = ServicePrefix + name;

            Assert.True(
                byService.TryGetValue(service, out var own),
                $"Профили называют gRPC-адрес {name}, а сервиса {service} в src/ нет."
            );

            Assert.Equal(port, Port(own!.Variables, "Grpc__GrpcPort", service));

            Assert.Equal(
                ApplicationPort(own.ApplicationUrl, own.Service),
                Port(own.Variables, "Grpc__HttpPort", service)
            );
        }
    }

    /// <summary>
    /// Карта адресов и gRPC-адресов у всех профилей одна: общий блок не разошёлся.
    /// </summary>
    /// <remarks>
    /// Пропущенный в общем блоке сосед проявляется не ошибкой, а пустой строкой
    /// подключения: сервис считает, что вызывать никого не нужно. Сверка карты
    /// целиком ловит и пропуск, и лишний адрес, которого ни у кого нет.
    /// </remarks>
    [Fact]
    public void КартаАдресовОднаУВсехПрофилей()
    {
        var profiles = ReadProfiles();

        var expected = EndpointPorts(profiles);
        var expectedGrpc = GrpcEndpointPorts(profiles);

        Assert.NotEmpty(expected);
        Assert.NotEmpty(expectedGrpc);

        Assert.All(
            profiles,
            profile =>
            {
                Assert.Equal(Pretty(expected), Pretty(EndpointPorts([profile])));
                Assert.Equal(Pretty(expectedGrpc), Pretty(GrpcEndpointPorts([profile])));
            }
        );
    }

    /// <summary>
    /// Порт, который сервис сам объявляет целью Prometheus, равен порту профиля.
    /// </summary>
    /// <remarks>
    /// Профиль — единственное место, откуда сервис на хосте узнаёт свой адрес для
    /// наблюдаемости: ключ <c>Dev__MetricsPort</c> задаёт генератор из строки
    /// <c>port=</c>. Расхождение с <c>applicationUrl</c> не роняет запуск — оно
    /// выглядит как пустые графики в Grafana, и искать пришлось бы в самой
    /// Grafana, а не в профиле.
    /// </remarks>
    [Fact]
    public void ПортPrometheusСовпадаетСПортомПрофиля()
    {
        foreach (var profile in ReadProfiles())
        {
            Assert.Equal(
                Port(profile.Variables, "Dev__MetricsPort", profile.Service),
                ApplicationPort(profile.ApplicationUrl, profile.Service)
            );
        }
    }

    /// <summary>
    /// Каталог файлов логов у всех сервисов один, относительный и под gitignore.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Alloy читает этот каталог, поэтому у сервисов он обязан совпадать: свой
    /// каталог у одного сервиса означал бы, что его логи не попадут в Loki вообще.
    /// </para>
    /// <para>
    /// Относительный путь — путь машины автора в общем файле не годится, а
    /// gitignore — потому что каталог наполняется логами отладки и однажды
    /// попал бы в коммит.
    /// </para>
    /// </remarks>
    [Fact]
    public void КаталогЛоговОдинИПодGitignore()
    {
        var ignored = GitIgnoreLines();
        var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var profile in ReadProfiles())
        {
            var configured = profile.Variables.TryGetValue("Dev__LogDirectory", out var value)
                ? value
                : string.Empty;

            Assert.False(
                string.IsNullOrWhiteSpace(configured),
                $"{profile.Service}: в профиле нет Dev__LogDirectory — логи сервиса на"
                    + " хосте останутся только в консоли."
            );

            Assert.False(
                Path.IsPathRooted(configured),
                $"{profile.Service}: каталог логов задан абсолютным путём: {configured}."
            );

            resolved.Add(
                Path.GetFullPath(Path.Combine(RepositoryRoot(), "src", profile.Service, configured))
            );
        }

        var root = Assert.Single(resolved);
        var relative = Path.GetRelativePath(RepositoryRoot(), root).Replace('\\', '/');

        Assert.StartsWith(RepositoryRoot(), root, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(relative + "/", ignored);
    }

    /// <summary>Секретные ключи в профиль не попали.</summary>
    /// <remarks>
    /// Значения этих ключей берутся из user-secrets: в репозитории их быть не
    /// должно ни подставленными, ни пустыми строками. Список — строки
    /// <c>#secrets</c> файла данных: ключ, который в профиль положить забыли, и
    /// ключ, который положили, но не объявили секретным, — разные ошибки, и
    /// вторая осталась бы незамеченной без этой сверки.
    /// </remarks>
    [Fact]
    public void СекретныхКлючейВПрофиляхНет()
    {
        var data = ReadLaunchData();
        var profiles = ReadProfiles();

        var secrets = data
            .Secrets.Values.SelectMany(line =>
                line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            )
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(secrets);

        foreach (var profile in profiles)
        {
            foreach (var secret in secrets)
            {
                Assert.False(
                    profile.Variables.ContainsKey(secret),
                    $"{profile.Service}: секретный ключ {secret} попал в launchSettings.json."
                );
            }
        }
    }

    /// <summary>
    /// Автостарт инфраструктуры описан одинаково в профиле и в скриптах.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Профиль — единственный источник флага <c>Dev:AutoStartInfra</c> для C#: в
    /// <c>appsettings.json</c> секции <c>Dev</c> нет намеренно (иначе автостарт
    /// включился бы и в контейнере). Значит ключ, пропущенный в общем блоке,
    /// выключает автостарт молча — ровно так, как он и был выключен до этой
    /// правки, и F5 снова ждёт базу минуту.
    /// </para>
    /// <para>
    /// Списки служб живут ещё и в <c>scripts/windows/common.ps1</c> /
    /// <c>scripts/unix/common.sh</c>: <c>local -Run</c> поднимает инфраструктуру и
    /// без F5. Сверка идёт «данные против скриптов», а не «данные против копии»:
    /// копия в тесте подтвердила бы саму себя. Иначе два пути запуска на хосте
    /// поднимали бы разные наборы, и расхождение выглядело бы как «через скрипт
    /// работает, через F5 нет».
    /// </para>
    /// </remarks>
    [Fact]
    public void АвтостартИнфраструктурыОписанОдинаковоВПрофилеИСкриптах()
    {
        var common = ReadLaunchData().Common;
        var scripts = ScriptInfra();

        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Dev__AutoStartInfra"] = "true",
            ["Dev__InfraServices"] = scripts.Services,
            ["Dev__InfraBackgroundServices"] = scripts.BackgroundServices,
            ["Dev__InfraWaitTimeoutSeconds"] = scripts.WaitTimeout,
        };

        foreach (var (key, value) in expected)
        {
            Assert.True(
                common.TryGetValue(key, out var configured),
                $"[common] не задаёт {key} — автостарт инфраструктуры выключен."
            );

            Assert.Equal(value, configured);
        }

        // Общий блок генератор кладёт в каждый профиль целиком — здесь и видно,
        // что ключи достались всем сервисам, а не только тому, который открыли.
        foreach (var profile in ReadProfiles())
        {
            foreach (var (key, value) in expected)
            {
                Assert.True(
                    profile.Variables.TryGetValue(key, out var configured),
                    $"{profile.Service}: в профиле MARS.Local нет {key}."
                );

                Assert.Equal(value, configured);
            }
        }
    }

    /// <summary>
    /// Списки автостарта из скриптов — в том же виде, что в профиле: имена через
    /// запятую, срок ожидания числом.
    /// </summary>
    /// <remarks>
    /// Windows держит список массивом PowerShell, unix — строкой через пробел,
    /// поэтому оба прочитанных набора обязаны совпасть и между собой: разойдись
    /// они, расхождение было бы видно только на одной платформе.
    /// </remarks>
    private static (string Services, string BackgroundServices, string WaitTimeout) ScriptInfra()
    {
        var windows = ReadScript("windows", "common.ps1");
        var unix = ReadScript("unix", "common.sh");

        var services = ServiceList(PsInfraServices().Match(windows).Groups["value"].Value);
        var background = ServiceList(
            PsInfraBackgroundServices().Match(windows).Groups["value"].Value
        );
        var wait = PsInfraWaitTimeout().Match(windows).Groups["value"].Value;

        Assert.Equal(services, ServiceList(ShInfraServices().Match(unix).Groups["value"].Value));
        Assert.Equal(
            background,
            ServiceList(ShInfraBackgroundServices().Match(unix).Groups["value"].Value)
        );
        Assert.Equal(wait, ShInfraWaitTimeout().Match(unix).Groups["value"].Value);

        Assert.False(string.IsNullOrWhiteSpace(services), "Скрипты не задают список служб.");
        Assert.False(string.IsNullOrWhiteSpace(wait), "Скрипты не задают срок ожидания.");

        return (services, background, wait);
    }

    /// <summary>Текст файла скриптов платформы.</summary>
    private static string ReadScript(string platform, string file) =>
        File.ReadAllText(ClientUiImageWorkflowTests.FindRepositoryFile("scripts", platform, file));

    /// <summary>
    /// Имена служб через запятую: запятые массива PowerShell и пробелы bash дают
    /// один и тот же список.
    /// </summary>
    private static string ServiceList(string value) =>
        string.Join(
            ",",
            value
                .Replace("\"", string.Empty, StringComparison.Ordinal)
                .Split(
                    [',', ' '],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                )
        );

    /// <summary>Блоки данных совпадают с набором сервисов: ни забытого, ни лишнего.</summary>
    /// <remarks>
    /// Забытый блок генератор отвергает сам, а вот блок сервиса, которого в
    /// <c>src/</c> нет, переживёт удаление каталога: список данных молча
    /// устаревает, и следующий сервис берёт настройки от умершего соседа.
    /// </remarks>
    [Fact]
    public void БлокиДанныхСовпадаютСНаборомСервисов()
    {
        var data = ReadLaunchData();

        Assert.Equal(
            ServiceDirectories().OrderBy(name => name, StringComparer.Ordinal),
            data.Services.Keys.OrderBy(name => name, StringComparer.Ordinal)
        );
    }

    /// <summary>Корень раздачи файлов у сервисов один и лежит внутри репозитория.</summary>
    /// <remarks>
    /// <para>
    /// <c>MARS.Alerts</c> пишет файлы в <c>&lt;корень&gt;/Alerts</c>, а
    /// <c>MARS.MediaStorage</c> раздаёт их оттуда же: в контейнере это общий том
    /// <c>mars-wwwroot</c>. Локальная отладка обязана воспроизводить ту же
    /// картину, иначе конвейер «алерт → файл» проверяется на двух разных папках:
    /// файл появляется, а раздача его не находит, и это выглядит как дефект
    /// раздачи, а не как разошедшиеся настройки.
    /// </para>
    /// <para>
    /// Путь обязан быть относительным (абсолютный был бы путём машины автора в
    /// общем файле), лежать внутри репозитория и попадать под <c>.gitignore</c>:
    /// иначе запуск оверлея добавляет медиафайлы в <c>git status</c>, и однажды
    /// их коммитят.
    /// </para>
    /// </remarks>
    [Fact]
    public void КореньРаздачиОдинИВнутриРепозитория()
    {
        var profiles = ReadProfiles();
        var ignored = GitIgnoreLines();
        var resolved = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var profile in profiles)
        {
            if (profile.Variables.TryGetValue("ASPNETCORE_WEBROOT", out var configured))
            {
                Assert.False(
                    Path.IsPathRooted(configured),
                    $"{profile.Service}: корень раздачи задан абсолютным путём: {configured}."
                );

                var full = Path.GetFullPath(
                    Path.Combine(RepositoryRoot(), "src", profile.Service, configured)
                );

                Assert.StartsWith(RepositoryRoot(), full, StringComparison.OrdinalIgnoreCase);

                resolved[profile.Service] = full;
            }
        }

        // Ровно два сервиса конвейера. Третий корень означал бы, что кто-то
        // раздаёт файлы из своей папки, и конвейер этого не заметит.
        Assert.Equal(
            new[] { "MARS.Alerts", "MARS.MediaStorage" },
            resolved.Keys.OrderBy(name => name, StringComparer.Ordinal)
        );

        var root = Assert.Single(resolved.Values.Distinct(StringComparer.OrdinalIgnoreCase));
        var relative = Path.GetRelativePath(RepositoryRoot(), root).Replace('\\', '/');

        Assert.Contains(relative + "/", ignored);
    }

    /// <summary>Строки <c>.gitignore</c>: без комментариев, без ведущего слэша.</summary>
    /// <remarks>
    /// Ведущий слэш убирается, потому что сравнивать надо путь, а не способ его
    /// записать: <c>/data/local-wwwroot/</c> и <c>data/local-wwwroot/</c> —
    /// правило об одном и том же.
    /// </remarks>
    private static List<string> GitIgnoreLines() =>
        File.ReadAllLines(ClientUiImageWorkflowTests.FindRepositoryFile(".gitignore"))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .Where(line => line[0] != '#' && line[0] != '!')
            .Select(line => line.TrimStart('/'))
            .ToList();

    /// <summary>Все подстановки профиля разрешает шаблон окружения.</summary>
    /// <remarks>
    /// Нера­зрешённая подстановка уехала бы в профиль строкой вида
    /// <c>${MARS_PORT_TTS}</c>, и сервис упал бы не на подстановке, а на
    /// подключении, — искать пришлось бы в другом месте. Тест проверяет две вещи:
    /// каждое имя из данных есть в шаблоне, а в собранных профилях подстановок не
    /// осталось.
    /// </remarks>
    [Fact]
    public void ПодстановкиПрофиляРазрешаютсяШаблоном()
    {
        var template = ReadTemplate();

        foreach (var (service, values) in AllValues(ReadLaunchData()))
        {
            foreach (var (key, value) in values)
            {
                foreach (Match match in Placeholder().Matches(value))
                {
                    var name = match.Groups["name"].Value;

                    Assert.True(
                        template.ContainsKey(name),
                        $"{service}: {key} ссылается на {name}, которого нет в .env.development.example."
                    );
                }
            }
        }

        foreach (var profile in ReadProfiles())
        {
            foreach (var (key, value) in profile.Variables)
            {
                Assert.False(
                    value.Contains("${", StringComparison.Ordinal),
                    $"{profile.Service}: в {key} осталась неразрешённая подстановка: {value}."
                );
            }
        }
    }

    /// <summary>
    /// Режим проверки генератора ловит правку профиля — и пропускает совпавший.
    /// </summary>
    /// <remarks>
    /// Проверка идёт на временном корне репозитория: генератор получает
    /// <c>--root</c>, поэтому подделка не касается рабочих файлов. Тест доказывает
    /// именно то, ради чего <c>--check</c> и существует: расхождение текста
    /// профиля с данными даёт код возврата 1, а не «ноль при любом содержании».
    /// Без Python (его нет в PATH) проверка пропускается: сам генератор на нём и
    /// написан, и подменять его копией на C# значило бы проверять копию.
    /// </remarks>
    [Fact]
    public void ПроверкаЛовитРасхождениеПрофиля()
    {
        var python = FindPython();

        if (python is not null)
        {
            var root = Directory.CreateTempSubdirectory("mars-local-check-");

            try
            {
                var profile = PrepareProbeRepository(root.FullName);

                Assert.Equal(0, RunPython(python, root.FullName, check: false));
                Assert.Equal(0, RunPython(python, root.FullName, check: true));

                var text = File.ReadAllText(profile);

                File.WriteAllText(
                    profile,
                    text.Replace(
                        "\"launchBrowser\": false",
                        "\"launchBrowser\": true",
                        StringComparison.Ordinal
                    )
                );

                Assert.Equal(1, RunPython(python, root.FullName, check: true));
            }
            finally
            {
                root.Delete(recursive: true);
            }
        }
        else
        {
            Assert.Skip("Ни python3, ни python не найдены — режим --check не проверяется.");
        }
    }

    /// <summary>
    /// Готовит временный корень: данные, шаблон окружения, каталог одного сервиса и копию генератора.
    /// </summary>
    private static string PrepareProbeRepository(string root)
    {
        var scripts = Directory.CreateDirectory(Path.Combine(root, "scripts")).FullName;
        var properties = Directory
            .CreateDirectory(Path.Combine(root, "src", "MARS.Probe", "Properties"))
            .FullName;

        File.Copy(
            ClientUiImageWorkflowTests.FindRepositoryFile("scripts/local_launch.py"),
            Path.Combine(scripts, "local_launch.py")
        );

        File.WriteAllText(
            Path.Combine(scripts, "local-launch.conf"),
            """
            [common]
            RabbitMq__Host=localhost
            RabbitMq__Port=${MARS_PORT_RABBITMQ}

            [MARS.Probe]
            port=${MARS_PORT_PROBE}
            """
        );

        File.WriteAllText(
            Path.Combine(root, ".env.development.example"),
            "MARS_PORT_RABBITMQ=6018\nMARS_PORT_PROBE=6002\n"
        );

        var profile = Path.Combine(properties, "launchSettings.json");

        File.WriteAllText(
            profile,
            """
            {
              "profiles": {
                "MARS.Probe": {
                  "commandName": "Project"
                }
              }
            }
            """
        );

        return profile;
    }

    /// <summary>Запускает генератор во временном корне и возвращает код возврата.</summary>
    private static int RunPython(string python, string root, bool check)
    {
        var startInfo = new ProcessStartInfo(python)
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add(Path.Combine("scripts", "local_launch.py"));
        startInfo.ArgumentList.Add("--root");
        startInfo.ArgumentList.Add(".");

        if (check)
        {
            startInfo.ArgumentList.Add("--check");
        }

        var process =
            Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Не удалось запустить {python}.");

        using (process)
        {
            process.WaitForExit();
            return process.ExitCode;
        }
    }

    /// <summary>Команда Python в PATH, либо <c>null</c>, если её нет.</summary>
    private static string? FindPython()
    {
        var result = default(string);

        foreach (var candidate in new[] { "python", "python3" })
        {
            var startInfo = new ProcessStartInfo(candidate)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            startInfo.ArgumentList.Add("--version");

            try
            {
                using var process = Process.Start(startInfo);

                if (process is not null)
                {
                    process.WaitForExit();

                    // Псевдоним-заглушка Microsoft Store запускается и выходит с
                    // 9009: команда «есть», а Python за ней нет.
                    if (process.ExitCode == 0)
                    {
                        result = candidate;
                        break;
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Команды нет — пробуем следующую.
            }
        }

        return result;
    }

    /// <summary>Порт, на который смотрит адрес соседа, по каждому имени.</summary>
    private static Dictionary<string, int> EndpointPorts(IEnumerable<ProfileView> profiles) =>
        PrefixPorts(profiles, EndpointPrefix);

    /// <summary>gRPC-порт по каждому имени сервиса, который его обслуживает.</summary>
    private static Dictionary<string, int> GrpcEndpointPorts(IEnumerable<ProfileView> profiles) =>
        PrefixPorts(profiles, GrpcEndpointPrefix);

    /// <summary>Карта «имя → порт» по ключам с данным префиксом, с проверкой повторов.</summary>
    /// <remarks>
    /// Согласие профилей проверяет вызывающий: здесь карта одного профиля, и
    /// расхождение между профилями видно как разные словари, а не как последний
    /// победивший. Число не разбирается молча: адрес обязан кончаться портом.
    /// </remarks>
    private static Dictionary<string, int> PrefixPorts(
        IEnumerable<ProfileView> profiles,
        string prefix
    )
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var profile in profiles)
        {
            foreach (var (key, value) in profile.Variables)
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    var name = key[prefix.Length..];

                    result[name] = UriOf(value).Port;
                }
            }
        }

        return result;
    }

    /// <summary>Все ключи данных профиля: общий блок и каждый сервис.</summary>
    private static IEnumerable<(string Service, Dictionary<string, string> Values)> AllValues(
        LaunchData data
    )
    {
        yield return ("common", data.Common);

        foreach (var (service, values) in data.Services)
        {
            yield return (service, values);
        }
    }

    /// <summary>Разбирает <c>scripts/local-launch.conf</c>.</summary>
    private static LaunchData ReadLaunchData()
    {
        var common = new Dictionary<string, string>(StringComparer.Ordinal);
        var services = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        var secrets = new Dictionary<string, string>(StringComparer.Ordinal);
        var currentName = "common";
        var current = common;

        foreach (
            var raw in File.ReadAllLines(
                ClientUiImageWorkflowTests.FindRepositoryFile("scripts/local-launch.conf")
            )
        )
        {
            var line = raw.Trim();

            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("#secrets ", StringComparison.Ordinal))
            {
                secrets[currentName] = line["#secrets ".Length..].Trim();
                continue;
            }

            if (line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith('['))
            {
                currentName = line.Trim('[', ']');

                if (currentName == "common")
                {
                    current = common;
                }
                else
                {
                    var values = new Dictionary<string, string>(StringComparer.Ordinal);

                    services[currentName] = values;
                    current = values;
                }

                continue;
            }

            var separator = line.IndexOf('=');

            Assert.True(separator > 0, $"Строка '{line}' не вида ключ=значение.");

            current[line[..separator].Trim()] = line[(separator + 1)..].Trim();
        }

        return new LaunchData(common, services, secrets);
    }

    /// <summary>Словарь «имя → порт» как отсортированный набор строк для сравнения.</summary>
    /// <remarks>
    /// Сравниваются строки, а не словари: у <see cref="Dictionary{TKey,TValue}"/>
    /// нет порядка, и «равны ли два словаря» пришлось бы доказывать обходом,
    /// который сам может разойтись с проверяемым.
    /// </remarks>
    private static List<string> Pretty(Dictionary<string, int> ports) =>
        ports
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => $"{entry.Key}={entry.Value}")
            .ToList();

    /// <summary>Шаблон окружения как словарь значений.</summary>
    private static Dictionary<string, string> ReadTemplate()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (
            var raw in File.ReadAllLines(
                ClientUiImageWorkflowTests.FindRepositoryFile(".env.development.example")
            )
        )
        {
            var line = raw.Trim();

            if (line.Length > 0 && line[0] != '#')
            {
                var separator = line.IndexOf('=');

                if (separator > 0)
                {
                    result[line[..separator].Trim()] = line[(separator + 1)..].Trim();
                }
            }
        }

        return result;
    }

    /// <summary>Профиль MARS.Local каждого сервиса из <c>src/</c>.</summary>
    private static List<ProfileView> ReadProfiles()
    {
        var result = new List<ProfileView>();

        foreach (var service in ServiceDirectories())
        {
            var text = File.ReadAllText(ProfilePath(service));

            using var document = JsonDocument.Parse(text);

            Assert.True(
                document.RootElement.TryGetProperty("profiles", out var profiles),
                $"{service}: в launchSettings.json нет раздела profiles."
            );

            Assert.True(
                profiles.TryGetProperty("MARS.Local", out var local),
                $"{service}: в launchSettings.json нет профиля MARS.Local."
            );

            Assert.True(
                local.TryGetProperty("environmentVariables", out var variables),
                $"{service}: у профиля MARS.Local нет ни одной переменной окружения."
            );

            Assert.True(
                local.TryGetProperty("applicationUrl", out var url),
                $"{service}: у профиля MARS.Local нет applicationUrl."
            );

            var values = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var property in variables.EnumerateObject())
            {
                values[property.Name] = property.Value.GetString() ?? string.Empty;
            }

            result.Add(new ProfileView(service, values, url.GetString() ?? string.Empty));
        }

        return result;
    }

    /// <summary>Имена сервисов <c>src/</c>, у которых есть <c>launchSettings.json</c>.</summary>
    /// <remarks>
    /// Наличие файла проверяется через <see cref="File.Exists"/>, а не через
    /// <see cref="ClientUiImageWorkflowTests.FindRepositoryFile"/>: второй на
    /// отсутствующем файле бросает исключение, и пропустить каталог без
    /// launchSettings (<c>MARS.Shared</c>) было бы нельзя.
    /// </remarks>
    private static List<string> ServiceDirectories() =>
        Directory
            .EnumerateDirectories(Path.Combine(RepositoryRoot(), "src"))
            .Where(service => File.Exists(ProfilePath(Path.GetFileName(service))))
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

    /// <summary>Путь файла <c>launchSettings.json</c> сервиса.</summary>
    private static string ProfilePath(string service) =>
        Path.Combine(RepositoryRoot(), "src", service, "Properties", "launchSettings.json");

    /// <summary>Корень репозитория: каталог, где лежит <c>MARS.slnx</c>.</summary>
    private static string RepositoryRoot() =>
        Path.GetDirectoryName(ClientUiImageWorkflowTests.FindRepositoryFile("MARS.slnx"))
        ?? throw new InvalidOperationException("Не удалось определить корень репозитория.");

    /// <summary>Целочисленное значение из переменных профиля.</summary>
    private static int Port(Dictionary<string, string> values, string key, string service)
    {
        Assert.True(values.TryGetValue(key, out var raw), $"{service}: нет переменной {key}.");

        Assert.True(
            int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var port),
            $"{service}: {key}={raw} — не целое число."
        );

        return port;
    }

    /// <summary>Первый адрес строки <c>applicationUrl</c> как <see cref="Uri"/>.</summary>
    private static Uri UriOf(string value)
    {
        var separator = value.IndexOf(';');
        var first = value.Contains(';', StringComparison.Ordinal) ? value[..separator] : value;

        Assert.True(
            Uri.TryCreate(first, UriKind.Absolute, out var uri),
            $"'{value}' — не абсолютный адрес."
        );

        return uri!;
    }

    /// <summary>Имя переменной внутри <c>${…}</c>.</summary>
    [GeneratedRegex(@"\$\{(?<name>[A-Za-z0-9_]+)\}")]
    private static partial Regex Placeholder();

    /// <summary>
    /// Адрес профиля запуска. Разбирается регуляркой, а не через Uri: хост «+»
    /// в адресе не является URI, поэтому Uri.TryCreate такой адрес отвергает, и
    /// проверка профиля падала бы не по делу.
    /// </summary>
    [GeneratedRegex(@"^(?<scheme>https?)://(?<host>[^:/?#]+):(?<port>\d+)$")]
    private static partial Regex ApplicationUrl();

    /// <summary>Порт из applicationUrl сервиса.</summary>
    private static int ApplicationPort(string applicationUrl, string service)
    {
        var match = ApplicationUrl().Match(applicationUrl);

        Assert.True(
            match.Success,
            $"{service}: '{applicationUrl}' — не адрес вида http://хост:порт."
        );

        return int.Parse(match.Groups["port"].Value, CultureInfo.InvariantCulture);
    }

    /// <summary>Список служб фазы 1 в common.ps1.</summary>
    [GeneratedRegex(@"\$script:MarsInfraServices = @\((?<value>[^)]*)\)")]
    private static partial Regex PsInfraServices();

    /// <summary>Список служб наблюдаемости в common.ps1.</summary>
    [GeneratedRegex(@"\$script:MarsInfraBackgroundServices = @\((?<value>[^)]*)\)")]
    private static partial Regex PsInfraBackgroundServices();

    /// <summary>Срок ожидания готовности в common.ps1.</summary>
    [GeneratedRegex(@"\$script:MarsInfraWaitTimeout = (?<value>\d+)")]
    private static partial Regex PsInfraWaitTimeout();

    /// <summary>Список служб фазы 1 в common.sh.</summary>
    [GeneratedRegex("MARS_INFRA_SERVICES=\"(?<value>[^\"]*)\"")]
    private static partial Regex ShInfraServices();

    /// <summary>Список служб наблюдаемости в common.sh.</summary>
    [GeneratedRegex("MARS_INFRA_BACKGROUND_SERVICES=\"(?<value>[^\"]*)\"")]
    private static partial Regex ShInfraBackgroundServices();

    /// <summary>Срок ожидания готовности в common.sh.</summary>
    [GeneratedRegex(@"MARS_INFRA_WAIT_TIMEOUT=(?<value>\d+)")]
    private static partial Regex ShInfraWaitTimeout();
}
