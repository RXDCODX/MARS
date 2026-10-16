using System.Text.RegularExpressions;
using MARS.Shared.Development;

namespace MARS.Gateway.Tests;

/// <summary>
/// Договорённости скриптов <c>scripts/windows</c> и <c>scripts/unix</c>.
/// </summary>
/// <remarks>
/// <para>
/// Скрипты на двух платформах — это два релиза одного набора, и именно поэтому
/// они расходятся тихо: описал <c>release.sh</c>, на Windows остался старый
/// <c>release.ps1</c>, и никто этого не заметил, потому что обе стороны
/// продолжают работать. CI тоже ничего не скажет: скрипты не входят ни в сборку,
/// ни в тестовую матрицу.
/// </para>
/// <para>Что проверяется и почему:</para>
/// <list type="bullet">
/// <item>
/// один набор действий на обеих платформах: разные действия на двух платформах
/// расходятся тихо;
/// </item>
/// <item>
/// корень репозитория ищется от каталога скрипта, а абсолютных путей машины нет:
/// скрипт, зашитый на <c>D:\VS\MARS</c>, работает у одного человека;
/// </item>
/// <item>
/// фильтры тестов остаются в синтаксисе Microsoft.Testing.Platform: фильтр
/// VSTest-вида находит ноль тестов и выходит с кодом 8, то есть выглядит как
/// «тестов нет», а не как ошибка;
/// </item>
/// <item>
/// фильтры покрытия в unix-скрипте совпадают с каноническими из
/// <c>.github/scripts/coverage-local.ps1</c>: локальное число покрытия должно
/// оставаться тем же, что в CI, а это тихо перестаёт выполняться при смене
/// одного фильтра;
/// </item>
/// <item>
/// списки образов и тестовых проектов читаются из репозитория, а не зашиты:
/// зашитый список однажды опубликовал бы лишний образ или промолчал бы про
/// забытый — ровно то, что уже было со сводкой релиза, где <c>client-ui</c> и
/// <c>shikimori</c> публиковались, но в итог не попадали;
/// </item>
/// <item>
/// merge и безусловный push не выполняются: скрипт не имеет права сливать ветку
/// или публиковать образы без явного указания.
/// </item>
/// </list>
/// <para>
/// Файлы читаются через <see cref="ClientUiImageWorkflowTests.FindRepositoryFile"/>:
/// копировать <c>scripts/**</c> в выход теста не нужно, а путь вглубь от
/// <c>bin/Release</c> рассыпался бы при смене конфигурации сборки.
/// </para>
/// </remarks>
public partial class ScriptsParityTests
{
    /// <summary>Каталоги скриптов: имя — платформа.</summary>
    private static readonly string[] Platforms = ["windows", "unix"];

    /// <summary>Расширение скрипта платформы.</summary>
    private static string ExtensionOf(string platform) => platform == "windows" ? ".ps1" : ".sh";

    /// <summary>Маркер, по которому скрипт узнаёт свой каталог.</summary>
    private static string SelfMarkerOf(string platform) =>
        platform == "windows" ? "$PSScriptRoot" : "BASH_SOURCE";

    /// <summary>Имя файла скрипта платформы по его имени без расширения.</summary>
    private static string FileNameOf(string platform, string name) => name + ExtensionOf(platform);

    /// <summary>Корень репозитория: каталог, где лежит MARS.slnx.</summary>
    private static string RepositoryRoot
    {
        get
        {
            var solution = ClientUiImageWorkflowTests.FindRepositoryFile("MARS.slnx");
            var directory = Path.GetDirectoryName(solution);

            Assert.False(
                string.IsNullOrEmpty(directory),
                $"Не удалось определить корень репозитория: {solution}"
            );

            return directory!;
        }
    }

    /// <summary>Каталог скриптов платформы: <c>scripts/windows</c>, <c>scripts/unix</c>.</summary>
    private static string ScriptsRoot(string platform) =>
        Path.Combine(RepositoryRoot, "scripts", platform);

    /// <summary>Имена скриптов платформы без расширения, по возрастанию.</summary>
    private static List<string> ScriptNames(string platform) =>
        Directory
            .EnumerateFiles(ScriptsRoot(platform))
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

    /// <summary>Текст скрипта.</summary>
    private static string Read(string platform, string name) =>
        File.ReadAllText(Path.Combine(ScriptsRoot(platform), FileNameOf(platform, name)));

    /// <summary>
    /// Все скрипты обеих платформ парами «платформа — имя», кроме общего кода
    /// платформы: <c>common</c> подключается, а не запускается.
    /// </summary>
    private static IEnumerable<(string Platform, string Name)> ActionScripts()
    {
        foreach (var platform in Platforms)
        {
            foreach (var name in ScriptNames(platform))
            {
                if (!string.Equals(name, "common", StringComparison.Ordinal))
                {
                    yield return (platform, name);
                }
            }
        }
    }

    [Fact]
    public void Набор_действий_совпадает_на_обеих_платформах()
    {
        var windows = ScriptNames("windows").Where(name => name != "common").ToList();
        var unix = ScriptNames("unix").Where(name => name != "common").ToList();

        Assert.NotEmpty(windows);
        Assert.Equal(windows, unix);
    }

    [Fact]
    public void У_обеих_платформ_есть_общий_код()
    {
        foreach (var platform in Platforms)
        {
            Assert.Contains("common", ScriptNames(platform));
        }
    }

    [Fact]
    public void Скрипт_ищет_корень_репозитория_от_себя()
    {
        foreach (var (platform, name) in ActionScripts())
        {
            var marker = SelfMarkerOf(platform);

            Assert.True(
                Read(platform, name).Contains(marker, StringComparison.Ordinal),
                $"{platform}/{FileNameOf(platform, name)} не ищет корень репозитория от каталога скрипта ({marker})."
                    + " Скрипт, зовущийся из Makefile или из IDE, работает иначе, чем вызванный из корня."
            );
        }
    }

    [Theory]
    [InlineData("D:\\")]
    [InlineData("C:\\Users")]
    [InlineData("/home/")]
    [InlineData("/Users/")]
    [InlineData("/mnt/c/")]
    public void В_скриптах_нет_путей_конкретной_машины(string machinePath)
    {
        foreach (var (platform, name) in ActionScripts())
        {
            Assert.False(
                Read(platform, name).Contains(machinePath, StringComparison.OrdinalIgnoreCase),
                $"{platform}/{FileNameOf(platform, name)} содержит путь машины: {machinePath}."
                    + " Скрипт с таким путём работает только у автора."
            );
        }
    }

    /// <summary>
    /// Скрипт тестов обязан оставаться в синтаксисе MTP.
    /// </summary>
    /// <remarks>
    /// Проверяется не подстрока <c>--filter</c> (она есть внутри
    /// <c>--filter-class</c>), а именно одиночный <c>--filter</c> — тот самый
    /// VSTest-видовый фильтр, который находит ноль тестов и выходит с кодом 8.
    /// </remarks>
    [Fact]
    public void Скрипт_тестов_не_использует_фильтр_вида_vstest()
    {
        foreach (var platform in Platforms)
        {
            var text = Read(platform, "test");
            var file = FileNameOf(platform, "test");

            var offender = StandaloneFilter()
                .Matches(text)
                .Select(match => match.Value)
                .FirstOrDefault();

            Assert.True(
                offender is null,
                $"{file} передаёт фильтр как `{offender}`. Так фильтр находит ноль тестов"
                    + " и завершается кодом 8 — это выглядит как «тестов нет», а не как ошибка."
                    + " Для MTP опции идут после `--`: --filter-class, --filter-method,"
                    + " --filter-namespace, --filter-trait."
            );

            Assert.Contains("--filter-class", text, StringComparison.Ordinal);
            Assert.Contains("--filter-method", text, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Фильтры coverlet в unix-скрипте те же, что в каноническом скрипте для
    /// Windows.
    /// </summary>
    /// <remarks>
    /// Unix-версии покрытия нет в <c>.github/scripts</c>, она живёт в
    /// <c>scripts/unix</c>, и её единственный способ остаться сравнимой с CI —
    /// повторять значения из <c>.github/scripts/coverage-local.ps1</c>. Значения
    /// берутся оттуда, а не зашиты в тест: иначе проверка защищала бы от
    /// расхождения с копией, а не с оригиналом.
    /// </remarks>
    [Fact]
    public void Фильтры_покрытия_unix_совпадают_с_каноническими()
    {
        var canonical = File.ReadAllText(
            ClientUiImageWorkflowTests.FindRepositoryFile(
                ".github",
                "scripts",
                "coverage-local.ps1"
            )
        );

        var unix = Read("unix", "coverage");

        foreach (
            var name in CoverletFilter()
                .Matches(canonical)
                .Select(match => match.Groups["value"].Value)
        )
        {
            Assert.True(
                unix.Contains(name, StringComparison.Ordinal),
                $"scripts/unix/coverage.sh не повторяет фильтр покрытия {name} из coverage-local.ps1."
                    + " Локальное покрытие перестанет быть сравнимым с CI, и это будет выглядеть"
                    + " как правдоподобный процент."
            );
        }
    }

    [Fact]
    public void Windows_скрипт_покрытия_делегирует_каноническому()
    {
        Assert.Contains(
            "coverage-local.ps1",
            Read("windows", "coverage"),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Скрипт_релиза_берёт_список_образов_из_workflow()
    {
        foreach (var platform in Platforms)
        {
            var text = Read(platform, "release");

            Assert.True(
                text.Contains("release-microservices.yml", StringComparison.Ordinal),
                $"Скрипт релиза {platform} не читает матрицу публикации."
                    + " Зашитый список однажды разойдётся с workflow — молча, как разошлась сводка релиза."
            );
        }
    }

    [Fact]
    public void Скрипт_релиза_не_пушит_без_явного_указания()
    {
        foreach (var platform in Platforms)
        {
            var text = Read(platform, "release");
            var file = FileNameOf(platform, "release");
            var marker = platform == "windows" ? "[switch]$Push" : "--push) push=";

            Assert.True(
                text.Contains(marker, StringComparison.Ordinal),
                $"{file} не объявляет явный переключатель публикации ({marker})."
                    + " Без него образы публиковались бы тем же запуском, который их собирает."
            );
        }
    }

    [Fact]
    public void Скрипт_пуша_ветки_не_сливает_её()
    {
        foreach (var platform in Platforms)
        {
            foreach (
                var line in Read(platform, "pr")
                    .Replace("\r\n", "\n", StringComparison.Ordinal)
                    .Split('\n')
            )
            {
                var command = line.TrimStart();

                // Строки-комментарии и Write-Note про merge есть и должны быть:
                // запрещено именно выполнение команды, а не упоминание.
                Assert.False(
                    MergeCommand().IsMatch(command),
                    $"{FileNameOf(platform, "pr")} выполняет merge: {command}."
                        + " Merge — решение владельца, скрипт открывает PR и на этом останавливается."
                );
            }
        }
    }

    /// <summary>
    /// У запуска сервиса на хосте один и тот же вход на обеих платформах:
    /// пересобрать профиль, поднять инфраструктуру, запустить сервис.
    /// </summary>
    /// <remarks>
    /// Проверяется наличие переключателя, а не его поведение: скрипты не входят ни
    /// в сборку, ни в тестовую матрицу, поэтому «на unix флага нет» не увидит
    /// никто. Платформа без <c>-Run</c> — это инструкция, работающая у половины
    /// команды, и обнаруживается уже на чужой машине.
    /// </remarks>
    [Fact]
    public void Запуск_сервиса_на_хосте_есть_на_обеих_платформах()
    {
        foreach (var platform in Platforms)
        {
            var text = Read(platform, "local");
            var file = FileNameOf(platform, "local");
            var marker = platform == "windows" ? "[string]$Run" : "--run)";

            Assert.True(
                text.Contains(marker, StringComparison.Ordinal),
                $"{file} не принимает сервис для запуска ({marker}). Без этого шаг"
                    + " «поднять инфраструктуру и запустить» остаётся ручной инструкцией."
            );

            Assert.True(
                text.Contains("--launch-profile", StringComparison.Ordinal),
                $"{file} не передаёт профиль запуска сервису: F5 берёт MARS.Local из"
                    + " launchSettings.json, а `dotnet run` требует его явно."
            );

            // Фаза базы и брокера обязана ждать готовности: сервис идёт в миграции
            // сразу за ней, а на холодном томе postgres отвечает не сразу.
            Assert.Contains("--wait", text, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Проверка паритета ключей файла окружения есть на обеих платформах и стоит
    /// до compose.
    /// </summary>
    /// <remarks>
    /// Отставший файл окружения compose не ломает: он подставляет пустую строку, и
    /// стенд поднимается со случайным хост-портом. Проверка, стоящая после
    /// compose, сообщила бы об этом уже по факту — то есть её можно было бы и не
    /// писать.
    /// </remarks>
    [Fact]
    public void Проверка_паритета_файла_окружения_идёт_до_compose()
    {
        foreach (var platform in Platforms)
        {
            var name =
                platform == "windows" ? "Test-MarsEnvironmentFileParity" : "test_env_file_parity";
            var common = Read(platform, "common");
            var stack = Read(platform, "stack");

            Assert.True(
                common.Contains(name, StringComparison.Ordinal),
                $"{FileNameOf(platform, "common")} не объявляет {name}: правка шаблона"
                    + " окружения разойдётся с файлом стенда молча."
            );

            var call = stack.IndexOf(name, StringComparison.Ordinal);
            var compose =
                platform == "windows"
                    ? stack.IndexOf("Invoke-MarsToolOrFail", StringComparison.Ordinal)
                    : stack.IndexOf("run_or_fail docker", StringComparison.Ordinal);

            Assert.True(
                call >= 0 && compose >= 0 && call < compose,
                $"{FileNameOf(platform, "stack")} не зовёт {name} до compose."
                    + " Так проверка сообщит о пустом порте после того, как стенд уже поднялся."
            );

            // Запуск сервиса на хосте — второй вход в ту же среду: поднимая её без
            // проверки, он повторяет ровно тот же отказ.
            Assert.True(
                Read(platform, "local").Contains(name, StringComparison.Ordinal),
                $"{FileNameOf(platform, "local")} поднимает инфраструктуру без {name}."
            );
        }
    }

    /// <summary>
    /// Списки автостарта инфраструктуры совпадают на обеих платформах.
    /// </summary>
    /// <remarks>
    /// Значения читаются из текста скриптов, а не зашиты в тест: он должен ловить
    /// расхождение платформ, а не сверять каждую с копией внутри себя. Совпадение
    /// с <c>scripts/local-launch.conf</c> (откуда те же списки читает C#-бутстрап)
    /// проверяет <c>LocalLaunchSettingsTests</c>.
    /// </remarks>
    [Fact]
    public void Списки_автостарта_инфраструктуры_совпадают_на_обеих_платформах()
    {
        var services = new List<List<string>>();
        var background = new List<List<string>>();
        var timeouts = new List<string>();

        foreach (var platform in Platforms)
        {
            var text = Read(platform, "common");
            var serviceRegex = platform == "windows" ? InfraPsServices() : InfraShServices();
            var backgroundRegex =
                platform == "windows" ? InfraPsBackgroundServices() : InfraShBackgroundServices();
            var timeoutRegex = platform == "windows" ? InfraPsTimeout() : InfraShTimeout();

            Assert.True(
                serviceRegex.IsMatch(text)
                    && backgroundRegex.IsMatch(text)
                    && timeoutRegex.IsMatch(text),
                $"{FileNameOf(platform, "common")} не объявляет списки автостарта инфраструктуры."
            );

            services.Add(SplitList(serviceRegex.Match(text).Groups["value"].Value));
            background.Add(SplitList(backgroundRegex.Match(text).Groups["value"].Value));
            timeouts.Add(timeoutRegex.Match(text).Groups["value"].Value);
        }

        Assert.NotEmpty(services[0]);
        Assert.NotEmpty(background[0]);
        Assert.Equal(services[0], services[1]);
        Assert.Equal(background[0], background[1]);
        Assert.Equal(timeouts[0], timeouts[1]);
    }

    /// <summary>
    /// Аргументы compose у C#-бутстрапа и у скриптов совпадают.
    /// </summary>
    /// <remarks>
    /// F5 не может позвать скрипт: профиль запускает приложение, а не bash, поэтому
    /// бутстрап собирает аргументы сам, и знание «какие флаги и файлы нужны»
    /// оказывается в двух местах. Разойдясь, они дали бы стенд, поднятый без
    /// dev-оверрайда, — и это выглядело бы как «через скрипт работает, через F5
    /// нет». Аргументы вынимаются из скриптов, а не сверяются с копией: копия в
    /// тесте подтвердила бы саму себя.
    /// </remarks>
    [Fact]
    public void Аргументы_compose_в_C_совпадают_со_скриптами()
    {
        var prefix = DevInfrastructureCompose
            .UpArguments(["маркер"], wait: false, waitTimeoutSeconds: 600)
            .TakeWhile(argument => argument != "up")
            .ToArray();

        foreach (var platform in Platforms)
        {
            Assert.Equal(ScriptComposeArguments(platform), prefix);
        }
    }

    /// <summary>
    /// Аргументы dev-стенда так, как их собирает скрипт платформы: общая часть
    /// <c>--env-file … -f … -f …</c> до подкоманды <c>up</c>.
    /// </summary>
    private static List<string> ScriptComposeArguments(string platform)
    {
        var text = Read(platform, "common");

        if (platform == "unix")
        {
            var value = ShComposeArguments().Match(text).Groups["value"].Value;

            Assert.False(
                string.IsNullOrWhiteSpace(value),
                "scripts/unix/common.sh не печатает аргументы dev-стенда."
            );

            return ["compose", .. value.Split(' ', StringSplitOptions.RemoveEmptyEntries)];
        }

        // PowerShell собирает тот же список массивом: `--env-file` берёт переменную
        // среды, а dev-оверрайд добавляется вторым `-f`.
        var environment = PsEnvironment().Match(text).Groups["value"].Value;
        var first = QuoteSeparated().Match(text).Groups["value"].Value;
        var second = QuoteSeparatedAppend().Match(text).Groups["value"].Value;

        Assert.False(
            string.IsNullOrWhiteSpace(environment) || string.IsNullOrWhiteSpace(first),
            "scripts/windows/common.ps1 не собирает аргументы dev-стенда."
        );

        return
        [
            "compose",
            .. SplitQuoted(first).Select(token => token == "$environment" ? environment : token),
            .. SplitQuoted(second),
        ];
    }

    /// <summary>Разбор элементов массива PowerShell: кавычки и запятые.</summary>
    private static List<string> SplitQuoted(string value) =>
        value
            .Replace("\"", string.Empty, StringComparison.Ordinal)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    /// <summary>Разбор списка служб: кавычки PowerShell и разделители пробелом.</summary>
    private static List<string> SplitList(string value) =>
        value
            .Replace("\"", string.Empty, StringComparison.Ordinal)
            .Split(
                [',', ' ', '\t'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
            .ToList();

    /// <summary>Службы автостарта в common.ps1.</summary>
    [GeneratedRegex(@"\$script:MarsInfraServices\s*=\s*@\((?<value>[^)]*)\)")]
    private static partial Regex InfraPsServices();

    /// <summary>Фоновые службы автостарта в common.ps1.</summary>
    [GeneratedRegex(@"\$script:MarsInfraBackgroundServices\s*=\s*@\((?<value>[^)]*)\)")]
    private static partial Regex InfraPsBackgroundServices();

    /// <summary>Ожидание готовности инфраструктуры в common.ps1.</summary>
    [GeneratedRegex(@"\$script:MarsInfraWaitTimeout\s*=\s*(?<value>\d+)")]
    private static partial Regex InfraPsTimeout();

    /// <summary>Службы автостарта в common.sh.</summary>
    [GeneratedRegex(@"MARS_INFRA_SERVICES=""(?<value>[^""]*)""")]
    private static partial Regex InfraShServices();

    /// <summary>Фоновые службы автостарта в common.sh.</summary>
    [GeneratedRegex(@"MARS_INFRA_BACKGROUND_SERVICES=""(?<value>[^""]*)""")]
    private static partial Regex InfraShBackgroundServices();

    /// <summary>Ожидание готовности инфраструктуры в common.sh.</summary>
    [GeneratedRegex(@"MARS_INFRA_WAIT_TIMEOUT=(?<value>\d+)")]
    private static partial Regex InfraShTimeout();

    /// <summary>
    /// Одиночный <c>--filter</c> как аргумент команды: внутри
    /// <c>--filter-class</c> он не считается, и обратными кавычками тоже.
    /// </summary>
    /// <remarks>
    /// Обратные кавычки исключены намеренно: объясняющий комментарий про
    /// VSTest-фильтр нужен (иначе ловушку придётся выяснять заново), а вот
    /// передавать его команде нельзя.
    /// </remarks>
    [GeneratedRegex(@"(?<![-\w`])--filter(?![-\w`])")]
    private static partial Regex StandaloneFilter();

    /// <summary>Команда merge в начале строки.</summary>
    [GeneratedRegex(@"^(gh\s+pr\s+merge|git\s+merge|git\s+push\b.*--force)")]
    private static partial Regex MergeCommand();

    /// <summary>Значения фильтров coverlet в каноническом скрипте покрытия.</summary>
    [GeneratedRegex(@"coverlet(?:Include|Exclude|ExcludeByFile)\s*=\s*'(?<value>[^']+)'")]
    private static partial Regex CoverletFilter();

    /// <summary>Печатаемые аргументы dev-стенда в common.sh.</summary>
    [GeneratedRegex("""(?<value>--env-file \.env\.development[^"]*)""")]
    private static partial Regex ShComposeArguments();

    /// <summary>Файл окружения dev-стенда в common.ps1.</summary>
    [GeneratedRegex("""if \(\$Dev\) \{ "(?<value>[^"]+)" \}""")]
    private static partial Regex PsEnvironment();

    /// <summary>Первый массив аргументов compose в common.ps1.</summary>
    [GeneratedRegex(@"\$files = @\((?<value>[^)]*)\)")]
    private static partial Regex QuoteSeparated();

    /// <summary>Добавление dev-оверрайда вторым файлом в common.ps1.</summary>
    [GeneratedRegex(@"\$files \+= @\((?<value>[^)]*)\)")]
    private static partial Regex QuoteSeparatedAppend();
}
