using System.Text.RegularExpressions;

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
}
